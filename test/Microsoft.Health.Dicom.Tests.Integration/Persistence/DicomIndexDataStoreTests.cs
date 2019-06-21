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
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomIndexDataStoreTests : IClassFixture<DicomCosmosDataStoreTestsFixture>
    {
        private readonly IDicomIndexDataStore _indexDataStore;

        public DicomIndexDataStoreTests(DicomCosmosDataStoreTestsFixture fixture)
        {
            _indexDataStore = fixture.DicomIndexDataStore;
        }

        [Fact]
        public async Task GivenAValidInstance_WhenIndexing_CanBeRetrieved()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();

            DicomDataset testInstance = CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID);

            await _indexDataStore.IndexInstanceAsync(testInstance);

            IEnumerable<DicomInstance> instancesInStudy = await _indexDataStore.GetInstancesInStudyAsync(studyInstanceUID);

            Assert.Single(instancesInStudy);
            Assert.True(testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID) == instancesInStudy.First().SopInstanceUID);

            IEnumerable<DicomInstance> instancesInSeries = await _indexDataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID);

            Assert.Single(instancesInSeries);
            Assert.True(testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID) == instancesInSeries.First().SopInstanceUID);

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

            IEnumerable<DicomInstance> instancesInStudy = await _indexDataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Equal(numberOfInstancesToIndex, instancesInStudy.Count());
            instancesInStudy.Each(x => Assert.True(sopInstanceUIDs.Contains(x.SopInstanceUID)));

            IEnumerable<DicomInstance> instancesInSeries = await _indexDataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(numberOfInstancesToIndex, instancesInSeries.Count());
            instancesInSeries.Each(x => Assert.True(sopInstanceUIDs.Contains(x.SopInstanceUID)));

            IEnumerable<DicomInstance> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(numberOfInstancesToIndex, instancesInSeries.Count());
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

            IEnumerable<DicomInstance> instancesInStudy = await _indexDataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Equal(numberOfInstancesToIndex - 1, instancesInStudy.Count());

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
            IndexDataStoreException deleteInstanceException = await Assert.ThrowsAsync<IndexDataStoreException>(
                () => _indexDataStore.DeleteInstanceIndexAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal(HttpStatusCode.NotFound, deleteInstanceException.StatusCode);

            IndexDataStoreException deleteSeriesException = await Assert.ThrowsAsync<IndexDataStoreException>(
                () => _indexDataStore.DeleteSeriesIndexAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal(HttpStatusCode.NotFound, deleteSeriesException.StatusCode);
        }

        [Fact]
        public async Task GivenNonExistentSeriesOrStudy_WhenFetchingInstances_IndexDataStoreExceptionIsThrownWithNotFoundStatusCode()
        {
            IndexDataStoreException getStudyInstancesException = await Assert.ThrowsAsync<IndexDataStoreException>(
                () => _indexDataStore.GetInstancesInStudyAsync(Guid.NewGuid().ToString()));
            Assert.Equal(HttpStatusCode.NotFound, getStudyInstancesException.StatusCode);

            IndexDataStoreException getSeriesInstancesException = await Assert.ThrowsAsync<IndexDataStoreException>(
                () => _indexDataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal(HttpStatusCode.NotFound, getSeriesInstancesException.StatusCode);
        }

        [Fact]
        public async Task GivenAStoredInstance_WhenStoringAgain_ConflictExceptionThrown()
        {
            DicomDataset testInstance = CreateTestInstanceDicomDataset(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            await _indexDataStore.IndexInstanceAsync(testInstance);
            IndexDataStoreException dataStoreException = await Assert.ThrowsAsync<IndexDataStoreException>(() => _indexDataStore.IndexInstanceAsync(testInstance));
            Assert.Equal(HttpStatusCode.Conflict, dataStoreException.StatusCode);

            await DeleteInstancesAsync(testInstance);
        }

        [Fact]
        public async Task GivenIndexDataStoreQuery_WhenQueryingWithInvalidParameters_ArgumentExceptionThrown()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.QueryStudiesAsync(-1, 10));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.QueryStudiesAsync(0, 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.QuerySeriesAsync(-1, 10));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.QuerySeriesAsync(0, 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.QueryInstancesAsync(-1, 10));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.QueryInstancesAsync(0, 0));
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
            (DicomTag, string)[] queryTags = new[] { (supportedDicomTag, testInstance1PatientName), (unsupportedDicomTag, "test") };

            IEnumerable<DicomStudy> studyResults = await _indexDataStore.QueryStudiesAsync(0, 10, studyInstanceUID, query: queryTags);
            Assert.Single(studyResults);
            Assert.Equal(new DicomStudy(dicomInstance.StudyInstanceUID), studyResults.First());

            IEnumerable<DicomSeries> seriesResults = await _indexDataStore.QuerySeriesAsync(0, 10, studyInstanceUID, query: queryTags);
            Assert.Single(seriesResults);
            Assert.Equal(new DicomSeries(dicomInstance.StudyInstanceUID, dicomInstance.SeriesInstanceUID), seriesResults.First());

            IEnumerable<DicomInstance> instanceResults = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID, query: queryTags);
            Assert.Single(instanceResults);
            Assert.Equal(dicomInstance, instanceResults.First());

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
            IEnumerable<DicomInstance> instances = await _indexDataStore.QueryInstancesAsync(0, 10, query: new[] { (DicomTag.ReferringPhysicianName, referringPhysicianName) });
            Assert.Single(instances);
            Assert.Equal(new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID), instances.First());

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
            IEnumerable<DicomInstance> queryInstances1 = await _indexDataStore.QueryInstancesAsync(numberOfInstances, numberOfInstances, studyInstanceUID);
            Assert.Empty(queryInstances1);

            // First 5 items
            IEnumerable<DicomInstance> queryInstances2 = await _indexDataStore.QueryInstancesAsync(0, numberOfInstances / 2, studyInstanceUID);
            Assert.Equal(numberOfInstances / 2, queryInstances2.Count());

            foreach (DicomInstance item in queryInstances2)
            {
                Assert.True(instanceUIDs.Remove(item.SopInstanceUID));
            }

            // Last 5 items
            IEnumerable<DicomInstance> queryInstances3 = await _indexDataStore.QueryInstancesAsync(numberOfInstances / 2, numberOfInstances, studyInstanceUID);
            Assert.Equal(numberOfInstances / 2, queryInstances3.Count());

            foreach (DicomInstance item in queryInstances3)
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
            IEnumerable<DicomStudy> queryStudiesResults1 = await _indexDataStore.QueryStudiesAsync(0, totalItems + 1);
            Assert.Equal(studyInstanceUIDs.Length, queryStudiesResults1.Count());
            queryStudiesResults1.Each(x => Assert.Contains(x.StudyInstanceUID, studyInstanceUIDs));

            IEnumerable<DicomStudy> queryStudiesResults2 = await _indexDataStore.QueryStudiesAsync(0, totalItems + 1, studyInstanceUIDs[0]);
            Assert.Single(queryStudiesResults2);

            IEnumerable<DicomStudy> queryStudiesResults3 = await _indexDataStore.QueryStudiesAsync(0, totalItems + 1, Guid.NewGuid().ToString());
            Assert.Empty(queryStudiesResults3);

            // Validate Series Searching
            IEnumerable<DicomSeries> querySeriesResults1 = await _indexDataStore.QuerySeriesAsync(0, totalItems + 1);
            Assert.Equal(dicomSeries.Count, querySeriesResults1.Count());
            querySeriesResults1.Each(x => Assert.Contains(x, dicomSeries));

            IEnumerable<DicomSeries> querySeriesResults2 = await _indexDataStore.QuerySeriesAsync(0, totalItems + 1, studyInstanceUIDs[0]);
            Assert.Equal(seriesPerStudy, querySeriesResults2.Count());

            IEnumerable<DicomSeries> querySeriesResults3 = await _indexDataStore.QuerySeriesAsync(0, totalItems + 1, Guid.NewGuid().ToString());
            Assert.Empty(querySeriesResults3);

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

            IEnumerable<DicomInstance> queryInstances1 = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID);
            Assert.Single(queryInstances1);
            Assert.Equal(DicomInstance.Create(testInstance), queryInstances1.First());

            IEnumerable<DicomInstance> queryInstances2 = await _indexDataStore.QueryInstancesAsync(0, 10, studyInstanceUID, new[] { (DicomTag.PatientName, " AND f.IndexedAttributes[\"0010,0010\"] = \"invalid\"") });
            Assert.Empty(queryInstances2);

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

            IEnumerable<DicomStudy> queryStudies1 = await _indexDataStore.QueryStudiesAsync(0, 10);
            Assert.Single(queryStudies1);
            Assert.Equal(studyInstanceUID, queryStudies1.First().StudyInstanceUID);

            IEnumerable<DicomStudy> queryStudies2 = await _indexDataStore.QueryStudiesAsync(0, 10, query: new[] { (DicomTag.PatientName, patientNames[numberOfItemsToInsert / 2]) });
            Assert.Single(queryStudies2);
            Assert.Equal(studyInstanceUID, queryStudies2.First().StudyInstanceUID);

            IEnumerable<DicomSeries> querySeries1 = await _indexDataStore.QuerySeriesAsync(0, 10);
            Assert.Single(querySeries1);
            Assert.Equal(seriesInstanceUID, querySeries1.First().SeriesInstanceUID);

            IEnumerable<DicomSeries> queryInstances1 = await _indexDataStore.QueryInstancesAsync(0, numberOfItemsToInsert + 1);
            Assert.Equal(numberOfItemsToInsert, queryInstances1.Count());

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
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
