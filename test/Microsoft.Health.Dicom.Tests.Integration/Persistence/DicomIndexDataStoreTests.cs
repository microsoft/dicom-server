// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
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
    }
}
