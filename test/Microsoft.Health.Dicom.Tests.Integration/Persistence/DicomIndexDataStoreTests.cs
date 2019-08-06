// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.CosmosDb.Features;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Microsoft.Health.Dicom.CosmosDb.Features.Transactions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomIndexDataStoreTests : IClassFixture<DicomCosmosDataStoreTestsFixture>
    {
        private readonly IDicomIndexDataStore _indexDataStore;
        private readonly DicomCosmosDataStoreTestsFixture _fixture;

        public DicomIndexDataStoreTests(DicomCosmosDataStoreTestsFixture fixture)
        {
            _indexDataStore = fixture.DicomIndexDataStore;
            _fixture = fixture;
        }

        [Fact]
        public async Task GivenAValidInstance_WhenIndexing_CanBeQueried()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();

            DicomDataset testInstance = CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID);

            await _indexDataStore.IndexInstanceAsync(testInstance);

            QueryResult<DicomInstance> instancesInStudy = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID);
            Assert.Single(instancesInStudy.Results);
            Assert.False(instancesInStudy.HasMoreResults);
            Assert.True(testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID) == instancesInStudy.Results.First().SopInstanceUID);

            QueryResult<DicomSeries> seriesInStudy = await _indexDataStore.QuerySeriesAsync(0, 10, studyInstanceUID);
            Assert.False(seriesInStudy.HasMoreResults);
            Assert.Single(seriesInStudy.Results);
            Assert.True(seriesInstanceUID == seriesInStudy.Results.First().SeriesInstanceUID);

            IEnumerable<DicomInstance> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);

            Assert.Single(deletedInstances);
            Assert.True(testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID) == deletedInstances.First().SopInstanceUID);
        }

        [Fact]
        public async Task GivenMultipleValidInstances_WhenIndexingInParallel_AreStored()
        {
            const int numberOfInstancesToIndex = 5;
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();

            IList<DicomDataset> instances = await CreateSeriesInParallelAsync(studyInstanceUID, seriesInstanceUID, numberOfInstancesToIndex);
            IList<string> sopInstanceUIDs = instances.Select(x => x.GetSingleValue<string>(DicomTag.SOPInstanceUID)).ToList();

            QueryResult<DicomInstance> instancesInStudy = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID);
            Assert.False(instancesInStudy.HasMoreResults);
            Assert.Equal(numberOfInstancesToIndex, instancesInStudy.Results.Count());
            instancesInStudy.Results.Each(x => Assert.True(sopInstanceUIDs.Contains(x.SopInstanceUID)));

            QueryResult<DicomSeries> seriesInStudy = await _indexDataStore.QuerySeriesAsync(0, 10, studyInstanceUID);
            Assert.Single(seriesInStudy.Results);
            Assert.False(seriesInStudy.HasMoreResults);
            Assert.True(seriesInstanceUID == seriesInStudy.Results.First().SeriesInstanceUID);

            IEnumerable<DicomInstance> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(numberOfInstancesToIndex, deletedInstances.Count());
            deletedInstances.Each(x => Assert.True(sopInstanceUIDs.Contains(x.SopInstanceUID)));
        }

        [Fact]
        public async Task GivenAValidSeries_WhenDeletingAnInstance_InstanceIsDeletedButSeriesRemains()
        {
            const int numberOfInstancesToIndex = 5;
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();

            IList<DicomDataset> instances = await CreateSeriesInParallelAsync(studyInstanceUID, seriesInstanceUID, numberOfInstancesToIndex);
            string firstSopInstanceUID = instances[0].GetSingleValue<string>(DicomTag.SOPInstanceUID);
            IList<string> otherSopInstanceUIDs = instances.Skip(1).Select(x => x.GetSingleValue<string>(DicomTag.SOPInstanceUID)).ToList();

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUID, seriesInstanceUID, firstSopInstanceUID);

            QueryResult<DicomInstance> instancesInStudy = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID);
            Assert.False(instancesInStudy.HasMoreResults);
            Assert.Equal(numberOfInstancesToIndex - 1, instancesInStudy.Results.Count());

            IEnumerable<DicomInstance> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
            deletedInstances.Each(x => Assert.True(otherSopInstanceUIDs.Contains(x.SopInstanceUID)));
        }

        [Fact]
        public async Task GivenAnInstanceWithInvalidInstanceId_WhenStoring_ExceptionIsThrown()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = "?/#";

            DicomDataset testInstance = CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID);

            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.IndexInstanceAsync(testInstance));

            testInstance.AddOrUpdate(DicomTag.SeriesInstanceUID, new string('a', 65));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.IndexInstanceAsync(testInstance));

            testInstance.Remove(DicomTag.SeriesInstanceUID);
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.IndexInstanceAsync(testInstance));
        }

        [Fact]
        public async Task GivenNonExistentInstancesOrSeries_WhenDeleting_IndexDataStoreExceptionIsThrownWithNotFoundStatusCode()
        {
            DataStoreException deleteInstanceException = await Assert.ThrowsAsync<DataStoreException>(
                () => _indexDataStore.DeleteInstanceIndexAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, deleteInstanceException.StatusCode);

            DataStoreException deleteSeriesException = await Assert.ThrowsAsync<DataStoreException>(
                () => _indexDataStore.DeleteSeriesIndexAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, deleteSeriesException.StatusCode);
        }

        [Fact]
        public async Task GivenNonExistentStudy_WhenQuerying_NoExceptionIsThrown()
        {
            QueryResult<DicomStudy> studyResults = await _indexDataStore.QueryStudiesAsync(0, 10, Guid.NewGuid().ToString());
            Assert.Empty(studyResults.Results);
            Assert.False(studyResults.HasMoreResults);

            QueryResult<DicomSeries> seriesResults = await _indexDataStore.QuerySeriesAsync(0, 10, Guid.NewGuid().ToString());
            Assert.Empty(seriesResults.Results);
            Assert.False(studyResults.HasMoreResults);

            QueryResult<DicomInstance> instanceResults = await _indexDataStore.QueryInstancesAsync(0, 10, Guid.NewGuid().ToString());
            Assert.Empty(instanceResults.Results);
            Assert.False(studyResults.HasMoreResults);
        }

        [Fact]
        public async Task GivenAStoredInstance_WhenStoringAgain_ConflictExceptionThrown()
        {
            DicomDataset testInstance = CreateTestInstanceDicomDataset(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            await _indexDataStore.IndexInstanceAsync(testInstance);
            DataStoreException dataStoreException = await Assert.ThrowsAsync<DataStoreException>(() => _indexDataStore.IndexInstanceAsync(testInstance));
            Assert.Equal((int)HttpStatusCode.Conflict, dataStoreException.StatusCode);

            await DeleteInstancesAsync(testInstance);
        }

        [Fact]
        public async Task GivenIndexDataStoreQuery_WhenQueryingWithInvalidParameters_ArgumentExceptionThrown()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _indexDataStore.QueryStudiesAsync(-1, 10));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _indexDataStore.QueryStudiesAsync(0, 0));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _indexDataStore.QuerySeriesAsync(-1, 10));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _indexDataStore.QuerySeriesAsync(0, 0));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _indexDataStore.QueryInstancesAsync(-1, 10));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _indexDataStore.QueryInstancesAsync(0, 0));
        }

        [Fact]
        public async Task GivenIndexDataStoreQuery_WhenQueryingOnNonIndexAttribute_IsIgnoredAndQueryReturns()
        {
            DicomTag unsupportedDicomTag = DicomTag.AbortFlag;
            DicomTag supportedDicomTag = DicomTag.PatientName;

            var studyInstanceUID = Guid.NewGuid().ToString();
            var testInstance1PatientName = "Test1";
            DicomDataset testInstance1 = CreateTestInstanceDicomDataset(studyInstanceUID, Guid.NewGuid().ToString(), testInstance1PatientName);
            DicomDataset testInstance2 = CreateTestInstanceDicomDataset(studyInstanceUID, Guid.NewGuid().ToString(), "Test2");

            testInstance1.AddOrUpdate(unsupportedDicomTag, "DifferentValue");

            await _indexDataStore.IndexInstanceAsync(testInstance1);
            await _indexDataStore.IndexInstanceAsync(testInstance2);

            var dicomInstance = DicomInstance.Create(testInstance1);
            (DicomAttributeId, string)[] queryAttributes = new[] { (new DicomAttributeId(supportedDicomTag), testInstance1PatientName), (new DicomAttributeId(unsupportedDicomTag), "test") };

            QueryResult<DicomStudy> studyResults = await _indexDataStore.QueryStudiesAsync(0, 10, studyInstanceUID, query: queryAttributes);
            Assert.Single(studyResults.Results);
            Assert.False(studyResults.HasMoreResults);
            Assert.Equal(new DicomStudy(dicomInstance.StudyInstanceUID), studyResults.Results.First());

            QueryResult<DicomSeries> seriesResults = await _indexDataStore.QuerySeriesAsync(0, 10, studyInstanceUID, query: queryAttributes);
            Assert.Single(seriesResults.Results);
            Assert.False(seriesResults.HasMoreResults);
            Assert.Equal(new DicomSeries(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID), seriesResults.Results.First());

            QueryResult<DicomInstance> instanceResults = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID, query: queryAttributes);
            Assert.Single(instanceResults.Results);
            Assert.False(instanceResults.HasMoreResults);
            Assert.Equal(dicomInstance, instanceResults.Results.First());

            await DeleteInstancesAsync(testInstance1, testInstance2);
        }

        [Fact]
        public async Task GivenIndexedInstance_WhenQueryingByPatientName_InstancesIsRetrieved()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();
            string referringPhysicianName = Guid.NewGuid().ToString();

            DicomDataset testInstance = CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID);
            testInstance.Add(DicomTag.ReferringPhysicianName, referringPhysicianName);

            await _indexDataStore.IndexInstanceAsync(testInstance);

            var sopInstanceUID = testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            QueryResult<DicomInstance> instances = await _indexDataStore.QueryInstancesAsync(0, 10, query: new[] { (new DicomAttributeId(DicomTag.ReferringPhysicianName), referringPhysicianName) });
            Assert.Single(instances.Results);
            Assert.False(instances.HasMoreResults);
            Assert.Equal(new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID), instances.Results.First());

            await DeleteInstancesAsync(testInstance);
        }

        [Fact]
        public async Task GivenIndexedSeries_WhenQueryingWithPaging_PagesReturnedCorrectly()
        {
            const int numberOfInstances = 20;
            Assert.Equal(0, numberOfInstances % 2);
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();

            IList<DicomDataset> instances = await CreateSeriesInParallelAsync(studyInstanceUID, seriesInstanceUID, numberOfInstances);
            var instanceUIDs = new HashSet<string>(instances.Select(x => x.GetSingleValue<string>(DicomTag.SOPInstanceUID)));

            // End of page
            QueryResult<DicomInstance> queryInstances1 = await _indexDataStore.QueryInstancesAsync(numberOfInstances, numberOfInstances, studyInstanceUID);
            Assert.Empty(queryInstances1.Results);
            Assert.False(queryInstances1.HasMoreResults);

            // First half of items items
            QueryResult<DicomInstance> queryInstances2 = await _indexDataStore.QueryInstancesAsync(0, numberOfInstances / 2, studyInstanceUID);
            Assert.Equal(numberOfInstances / 2, queryInstances2.Results.Count());
            Assert.False(queryInstances2.HasMoreResults);

            foreach (DicomInstance item in queryInstances2.Results)
            {
                Assert.True(instanceUIDs.Remove(item.SopInstanceUID));
            }

            // Last half of items
            QueryResult<DicomInstance> queryInstances3 = await _indexDataStore.QueryInstancesAsync(numberOfInstances / 2, numberOfInstances, studyInstanceUID);
            Assert.Equal(numberOfInstances / 2, queryInstances3.Results.Count());
            Assert.False(queryInstances3.HasMoreResults);

            foreach (DicomInstance item in queryInstances3.Results)
            {
                Assert.True(instanceUIDs.Remove(item.SopInstanceUID));
            }

            Assert.Empty(instanceUIDs);

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
        }

        [Fact]
        public async Task GivenMultipleStudies_WhenQueryingWithOrWithoutStudyInstanceUID_QueryResultsReturnedCorrectly()
        {
            var seriesPerStudy = 2;
            var instancesPerSeries = 2;
            var patientName = Guid.NewGuid().ToString();
            var studyInstanceUIDs = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var dicomSeries = new List<DicomSeries>();
            var totalItems = studyInstanceUIDs.Length * seriesPerStudy * instancesPerSeries;

            foreach (string studyInstanceUID in studyInstanceUIDs)
            {
                for (var i = 0; i < seriesPerStudy; i++)
                {
                    var series = new DicomSeries(studyInstanceUID, Guid.NewGuid().ToString());
                    dicomSeries.Add(series);

                    for (int ii = 0; ii < instancesPerSeries; ii++)
                    {
                        await _indexDataStore.IndexInstanceAsync(CreateTestInstanceDicomDataset(studyInstanceUID, series.SeriesInstanceUID, patientName));
                    }
                }
            }

            // Validate Study Searching
            QueryResult<DicomStudy> queryStudiesResults1 = await _indexDataStore.QueryStudiesAsync(0, totalItems + 1);
            Assert.Equal(studyInstanceUIDs.Length, queryStudiesResults1.Results.Count());
            Assert.False(queryStudiesResults1.HasMoreResults);
            queryStudiesResults1.Results.Each(x => Assert.Contains(x.StudyInstanceUID, studyInstanceUIDs));

            QueryResult<DicomStudy> queryStudiesResults2 = await _indexDataStore.QueryStudiesAsync(0, totalItems + 1, studyInstanceUIDs[0]);
            Assert.Single(queryStudiesResults2.Results);
            Assert.False(queryStudiesResults2.HasMoreResults);

            QueryResult<DicomStudy> queryStudiesResults3 = await _indexDataStore.QueryStudiesAsync(0, totalItems + 1, Guid.NewGuid().ToString());
            Assert.Empty(queryStudiesResults3.Results);
            Assert.False(queryStudiesResults3.HasMoreResults);

            // Validate Series Searching
            QueryResult<DicomSeries> querySeriesResults1 = await _indexDataStore.QuerySeriesAsync(0, totalItems + 1);
            Assert.Equal(dicomSeries.Count, querySeriesResults1.Results.Count());
            Assert.False(querySeriesResults1.HasMoreResults);
            querySeriesResults1.Results.Each(x => Assert.Contains(x, dicomSeries));

            QueryResult<DicomSeries> querySeriesResults2 = await _indexDataStore.QuerySeriesAsync(0, totalItems + 1, studyInstanceUIDs[0]);
            Assert.Equal(seriesPerStudy, querySeriesResults2.Results.Count());
            Assert.False(querySeriesResults2.HasMoreResults);

            QueryResult<DicomSeries> querySeriesResults3 = await _indexDataStore.QuerySeriesAsync(0, totalItems + 1, Guid.NewGuid().ToString());
            Assert.Empty(querySeriesResults3.Results);
            Assert.False(querySeriesResults3.HasMoreResults);

            foreach (DicomSeries series in dicomSeries)
            {
                await _indexDataStore.DeleteSeriesIndexAsync(series.StudyInstanceUID, series.SeriesInstanceUID);
            }
        }

        [Fact]
        public async Task GivenInstanceWithInjectedSql_WhenQuerying_IsReturnedCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();

            DicomDataset testInstance = CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID);
            testInstance.Add(DicomTag.ReferringPhysicianName, " AND f.IndexedAttributes[\"0010,0010\"] = \"invalid\"");

            await _indexDataStore.IndexInstanceAsync(testInstance);

            QueryResult<DicomInstance> queryInstances1 = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID);
            Assert.False(queryInstances1.HasMoreResults);
            Assert.Single(queryInstances1.Results);
            Assert.Equal(DicomInstance.Create(testInstance), queryInstances1.Results.First());

            QueryResult<DicomInstance> queryInstances2 = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID, new[] { (new DicomAttributeId(DicomTag.PatientName), " AND f.IndexedAttributes[\"0010,0010\"] = \"invalid\"") });
            Assert.False(queryInstances2.HasMoreResults);
            Assert.Empty(queryInstances2.Results);

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
        }

        [Fact]
        public async Task GivenWorstCaseStoreScenario_WhenAllInstancesHaveDifferentTagValues_CanIndexAndQueryCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();
            var startDateTime = new DateTime(2019, 6, 21);
            const int numberOfItemsToInsert = 100;
            var patientNames = Enumerable.Range(0, numberOfItemsToInsert).Select(_ => Guid.NewGuid().ToString()).ToArray();

            for (var i = 0; i < numberOfItemsToInsert; i++)
            {
                DicomDataset dataset = CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID, patientNames[i]);
                dataset.AddOrUpdate(DicomTag.StudyDate, startDateTime.AddDays(i));
                await _indexDataStore.IndexInstanceAsync(dataset);
            }

            QueryResult<DicomStudy> queryStudies1 = await _indexDataStore.QueryStudiesAsync(0, 10);
            Assert.False(queryStudies1.HasMoreResults);
            Assert.Single(queryStudies1.Results);
            Assert.Equal(studyInstanceUID, queryStudies1.Results.First().StudyInstanceUID);

            QueryResult<DicomStudy> queryStudies2 = await _indexDataStore.QueryStudiesAsync(0, 10, query: new[] { (new DicomAttributeId(DicomTag.PatientName), patientNames[numberOfItemsToInsert / 2]) });
            Assert.Single(queryStudies2.Results);
            Assert.False(queryStudies2.HasMoreResults);
            Assert.Equal(studyInstanceUID, queryStudies2.Results.First().StudyInstanceUID);

            QueryResult<DicomSeries> querySeries1 = await _indexDataStore.QuerySeriesAsync(0, 10);
            Assert.False(querySeries1.HasMoreResults);
            Assert.Single(querySeries1.Results);
            Assert.Equal(seriesInstanceUID, querySeries1.Results.First().SeriesInstanceUID);

            QueryResult<DicomInstance> queryInstances1 = await _indexDataStore.QueryInstancesAsync(0, numberOfItemsToInsert + 1);
            Assert.False(queryInstances1.HasMoreResults);
            Assert.Equal(numberOfItemsToInsert, queryInstances1.Results.Count());

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
        }

        [Fact]
        public async Task GivenIndexedStudyWithMultipleSeries_WhenDeletingStudy_AllSeriesAreDeleted()
        {
            const int numberOfSeriesPerStudy = 2;
            const int numberOfInstancesPerSeries = 100;
            IEnumerable<DicomDataset> study = await CreateStudyAsync(numberOfStudies: 1, numberOfSeriesPerStudy, numberOfInstancesPerSeries);
            DicomInstance[] dicomInstances = study.Select(x => DicomInstance.Create(x)).ToArray();

            var studyToDelete = dicomInstances[0].StudyInstanceUID;
            QueryResult<DicomSeries> querySeries = await _indexDataStore.QuerySeriesAsync(0, int.MaxValue, studyInstanceUID: studyToDelete);
            Assert.Equal(numberOfSeriesPerStudy, querySeries.Results.Count());

            IEnumerable<DicomInstance> deletedInstances = await _indexDataStore.DeleteStudyIndexAsync(studyToDelete);
            Assert.Equal(numberOfInstancesPerSeries * numberOfSeriesPerStudy, deletedInstances.Count());
            var expectedDeletedInstances = new HashSet<DicomInstance>(dicomInstances.Where(x => x.StudyInstanceUID == studyToDelete));

            foreach (DicomInstance deletedInstance in deletedInstances)
            {
                Assert.Contains(deletedInstance, expectedDeletedInstances);
            }

            querySeries = await _indexDataStore.QuerySeriesAsync(0, int.MaxValue, studyInstanceUID: studyToDelete);
            Assert.Empty(querySeries.Results);
        }

        [Fact]
        public async Task GivenValidDocumentAndMissingDocument_WhenDeletingUsingATransaction_EntireTransactionFailsAndNothingDeleted()
        {
            var validDocument = new QuerySeriesDocument(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var requestOptions = new RequestOptions() { PartitionKey = new PartitionKey(validDocument.PartitionKey) };
            validDocument = await _fixture.DocumentClient.GetOrCreateDocumentAsync(_fixture.DatabaseId, _fixture.CollectionId, validDocument.Id, requestOptions, validDocument);

            // Missing document
            using (ITransaction transaction = _fixture.DocumentClient.CreateTransaction(_fixture.DatabaseId, _fixture.CollectionId, requestOptions))
            {
                transaction.DeleteDocument(validDocument.Id, validDocument.ETag);
                transaction.DeleteDocument(Guid.NewGuid().ToString(), validDocument.ETag);
                await Assert.ThrowsAnyAsync<DocumentClientException>(() => transaction.CommitAsync());
            }

            // Invalid ETag
            using (ITransaction transaction = _fixture.DocumentClient.CreateTransaction(_fixture.DatabaseId, _fixture.CollectionId, requestOptions))
            {
                transaction.DeleteDocument(validDocument.Id, Guid.NewGuid().ToString());
                await Assert.ThrowsAnyAsync<DocumentClientException>(() => transaction.CommitAsync());
            }

            // Check initial document still exists.
            Uri documentUri = UriFactory.CreateDocumentUri(_fixture.DatabaseId, _fixture.CollectionId, validDocument.Id);
            DocumentResponse<QuerySeriesDocument> documentResponse = await _fixture.DocumentClient.ReadDocumentAsync<QuerySeriesDocument>(documentUri, requestOptions);

            Assert.NotNull(documentResponse.Document);
            Assert.Equal(validDocument.ETag, documentResponse.Document.ETag);
        }

        private static DicomDataset CreateTestInstanceDicomDataset(string studyInstanceUID, string seriesInstanceUID, string patientName = "Patient Test")
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID);
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID);

            var result = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.PatientName, patientName },
                { DicomTag.StudyDate, new DateTime(2019, 6, 21) },
            };
            return result;
        }

        private async Task<IEnumerable<DicomDataset>> CreateStudyAsync(
            int numberOfStudies = 1, int numberOfSeriesPerStudy = 1, int numberOfInstancesPerSeries = 100)
        {
            var indexedDatasets = new List<DicomDataset>(numberOfStudies * numberOfSeriesPerStudy * numberOfInstancesPerSeries);
            for (var studyIndex = 0; studyIndex < numberOfStudies; studyIndex++)
            {
                var studyInstanceUID = Guid.NewGuid().ToString();
                for (var seriesIndex = 0; seriesIndex < numberOfSeriesPerStudy; seriesIndex++)
                {
                    var seriesInstanceUID = Guid.NewGuid().ToString();
                    var series = Enumerable.Range(0, numberOfInstancesPerSeries)
                                    .Select(x => CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID)).ToArray();
                    await _indexDataStore.IndexSeriesAsync(series);

                    indexedDatasets.AddRange(series);
                }
            }

            return indexedDatasets;
        }

        private async Task<IList<DicomDataset>> CreateSeriesInParallelAsync(string studyInstanceUID, string seriesInstanceUID, int numberOfItemsInSeries)
        {
            IList<DicomDataset> instances = Enumerable.Range(0, numberOfItemsInSeries)
                                                        .Select(_ => CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID))
                                                        .ToList();
            await Task.WhenAll(instances.Select(x => _indexDataStore.IndexInstanceAsync(x)));
            return instances;
        }

        private async Task DeleteInstancesAsync(params DicomDataset[] datasets)
        {
            foreach (DicomDataset instance in datasets)
            {
                var dicomInstance = DicomInstance.Create(instance);
                await _indexDataStore.DeleteInstanceIndexAsync(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID, dicomInstance.SopInstanceUID);
            }
        }
    }
}
