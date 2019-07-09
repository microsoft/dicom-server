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

        [Fact]
        public async Task GivenAStudyWithInconsistentMetadata_WhenAddedAndDeletedFromMetadataStore_CanRetrieveTheStudyMetadataCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();
            DicomDataset instance1 = CreateInstanceTestDataset(studyInstanceUID, seriesInstanceUID, DateTime.UtcNow.AddDays(-1), "TestPatient1");
            DicomDataset instance2 = CreateInstanceTestDataset(studyInstanceUID, seriesInstanceUID, DateTime.UtcNow.AddDays(-3), "TestPatient2");

            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance1);
            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance2);

            DicomDataset studyMetadata1 = await _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID);
            Assert.Equal(studyInstanceUID, studyMetadata1.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(1, studyMetadata1.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedSeries));
            Assert.Equal(2, studyMetadata1.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedInstances));
            Assert.Equal(instance1.GetSingleValue<string>(DicomTag.PatientName), studyMetadata1.GetSingleValue<string>(DicomTag.PatientName));
            Assert.Equal(instance1.GetSingleValue<DateTime>(DicomTag.StudyDate), studyMetadata1.GetSingleValue<DateTime>(DicomTag.StudyDate));
            Assert.Equal(instance1.GetSingleValue<DateTime>(DicomTag.StudyTime), studyMetadata1.GetSingleValue<DateTime>(DicomTag.StudyTime));

            await _dicomMetadataStore.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, instance1.GetSingleValue<string>(DicomTag.SOPInstanceUID));

            DicomDataset studyMetadata2 = await _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID);
            Assert.Equal(studyInstanceUID, studyMetadata2.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(1, studyMetadata2.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedSeries));
            Assert.Equal(1, studyMetadata2.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedInstances));
            Assert.Equal(instance2.GetSingleValue<string>(DicomTag.PatientName), studyMetadata2.GetSingleValue<string>(DicomTag.PatientName));
            Assert.Equal(instance2.GetSingleValue<DateTime>(DicomTag.StudyDate), studyMetadata2.GetSingleValue<DateTime>(DicomTag.StudyDate));
            Assert.Equal(instance2.GetSingleValue<DateTime>(DicomTag.StudyTime), studyMetadata2.GetSingleValue<DateTime>(DicomTag.StudyTime));
        }

        [Fact]
        public async Task GivenAStudy_WhenAddedMetadataStore_CanRetrieveTheStudyMetadataCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            int numberSeries = 2;
            int numberOfInstancesPerSeries = 5;
            IList<DicomDataset> study = CreateStudyDataset(studyInstanceUID, numberSeries, numberOfInstancesPerSeries).ToList();

            foreach (DicomDataset dataset in study)
            {
                await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(dataset);
            }

            DicomDataset studyMetadata = await _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID, new HashSet<DicomAttributeId>() { new DicomAttributeId(DicomTag.PatientName) });
            Assert.NotNull(studyMetadata);
            Assert.Equal(studyInstanceUID, studyMetadata.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(numberSeries, studyMetadata.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedSeries));
            Assert.Equal(numberSeries * numberOfInstancesPerSeries, studyMetadata.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedInstances));
            Assert.Equal(study[0].GetSingleValue<string>(DicomTag.PatientName), studyMetadata.GetSingleValue<string>(DicomTag.PatientName));
            Assert.Equal(study[0].GetSingleValue<DateTime>(DicomTag.StudyDate), studyMetadata.GetSingleValue<DateTime>(DicomTag.StudyDate));
            Assert.Equal(study[0].GetSingleValue<DateTime>(DicomTag.StudyTime), studyMetadata.GetSingleValue<DateTime>(DicomTag.StudyTime));
        }

        private IEnumerable<DicomDataset> CreateStudyDataset(string studyInstanceUID, int seriesInStudy = 2, int instancesInSeries = 5)
        {
            DateTime studyDateTime = DateTime.UtcNow;
            var patientName = Guid.NewGuid().ToString();

            for (int series = 0; series < seriesInStudy; series++)
            {
                string seriesInstanceUID = Guid.NewGuid().ToString();

                for (int instance = 0; instance < instancesInSeries; instance++)
                {
                    yield return CreateInstanceTestDataset(studyInstanceUID, seriesInstanceUID, studyDateTime, patientName);
                }
            }
        }

        private DicomDataset CreateInstanceTestDataset(
            string studyInstanceUID, string seriesInstanceUID, DateTime studyDateTime, string patientName)
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.StudyDate, studyDateTime },
                { DicomTag.StudyTime, studyDateTime },
                { DicomTag.PatientName, patientName },
            };
        }
    }
}
