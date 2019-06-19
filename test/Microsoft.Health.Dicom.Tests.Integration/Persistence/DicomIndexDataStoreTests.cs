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

            IEnumerable<DicomIdentity> instancesInStudy = await _indexDataStore.GetInstancesInStudyAsync(studyInstanceUID);

            Assert.Single(instancesInStudy);
            Assert.True(testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID) == instancesInStudy.First().SopInstanceUID);

            IEnumerable<DicomIdentity> instancesInSeries = await _indexDataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID);

            Assert.Single(instancesInSeries);
            Assert.True(testInstance.GetSingleValue<string>(DicomTag.SOPInstanceUID) == instancesInSeries.First().SopInstanceUID);

            IEnumerable<DicomIdentity> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);

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

            IEnumerable<DicomIdentity> instancesInStudy = await _indexDataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Equal(numberOfInstancesToIndex, instancesInStudy.Count());
            instancesInStudy.Each(x => Assert.True(sopInstanceUIDs.Contains(x.SopInstanceUID)));

            IEnumerable<DicomIdentity> instancesInSeries = await _indexDataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(numberOfInstancesToIndex, instancesInSeries.Count());
            instancesInSeries.Each(x => Assert.True(sopInstanceUIDs.Contains(x.SopInstanceUID)));

            IEnumerable<DicomIdentity> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
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

            IEnumerable<DicomIdentity> instancesInStudy = await _indexDataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Equal(numberOfInstancesToIndex - 1, instancesInStudy.Count());

            IEnumerable<DicomIdentity> deletedInstances = await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUID, seriesInstanceUID);
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

        private static DicomDataset CreateTestInstanceDicomDataset(string studyInstanceUID, string seriesInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID);
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUID);

            var result = new DicomDataset();
            result.Add(DicomTag.StudyInstanceUID, studyInstanceUID);
            result.Add(DicomTag.SeriesInstanceUID, seriesInstanceUID);
            result.Add(DicomTag.SOPInstanceUID, Guid.NewGuid().ToString());
            return result;
        }

        private async Task<IList<DicomDataset>> CreateSeriesInParallelAsync(string studyInstanceUID, string seriesInstanceUID, int numberOfItemsInSeries)
        {
            IList<DicomDataset> instances = new List<DicomDataset>();

            for (var i = 0; i < numberOfItemsInSeries; i++)
            {
                instances.Add(CreateTestInstanceDicomDataset(studyInstanceUID, seriesInstanceUID));
            }

            await Task.WhenAll(instances.Select(x => _indexDataStore.IndexInstanceAsync(x)));
            return instances;
        }
    }
}
