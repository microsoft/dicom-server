// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomMetadataStoreTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public DicomMetadataStoreTests(DicomBlobStorageTestsFixture fixture)
        {
            _dicomMetadataStore = fixture.DicomMetadataStore;
        }

        [Fact]
        public async Task GivenAStudy_WhenAddedMetadataStore_CanRetrieveTheCorrectInstancesInTheStudyAndSeries()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            IList<DicomDataset> study = CreateStudyDataset(studyInstanceUID).ToList();
            var expectedInstances = new HashSet<DicomInstance>(study.Select(x => DicomInstance.Create(x)));

            foreach (DicomDataset dataset in study)
            {
                await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(dataset);
            }

            IEnumerable<DicomInstance> instancesInStudy = await _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Equal(expectedInstances.Count, instancesInStudy.Count());

            foreach (DicomInstance actualInstance in instancesInStudy)
            {
                Assert.Contains(actualInstance, expectedInstances);
            }

            var seriesInstanceGrouping = expectedInstances
                .GroupBy(x => x.SeriesInstanceUID)
                .ToDictionary(x => x.Key, x => new HashSet<string>(x.Select(y => y.SopInstanceUID)));

            foreach (KeyValuePair<string, HashSet<string>> seriesGroup in seriesInstanceGrouping)
            {
                IEnumerable<DicomInstance> instancesInSeries = await _dicomMetadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesGroup.Key);
                Assert.Equal(seriesGroup.Value.Count, instancesInSeries.Count());

                foreach (DicomInstance actualInstance in instancesInSeries)
                {
                    Assert.Contains(actualInstance.SopInstanceUID, seriesGroup.Value);
                }
            }
        }

        private IEnumerable<DicomDataset> CreateStudyDataset(string studyInstanceUID, int seriesInStudy = 2, int instancesInSeries = 5)
        {
            for (int series = 0; series < seriesInStudy; series++)
            {
                string seriesInstanceUID = Guid.NewGuid().ToString();

                for (int instance = 0; instance < instancesInSeries; instance++)
                {
                    yield return new DicomDataset()
                    {
                        { DicomTag.StudyInstanceUID, studyInstanceUID },
                        { DicomTag.SeriesInstanceUID, seriesInstanceUID },
                        { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
                    };
                }
            }
        }
    }
}
