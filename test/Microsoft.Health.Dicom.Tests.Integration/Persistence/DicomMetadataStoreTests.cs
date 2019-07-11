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
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
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
        public async Task GivenAnInvalidStudyOrSeriesInstanceUID_WhenFetchingStudyOrSeriesMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            DataStoreException exception1 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception1.StatusCode);

            DataStoreException exception2 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.GetStudyDicomMetadataAsync(Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception2.StatusCode);

            DataStoreException exception3 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception3.StatusCode);

            DataStoreException exception4 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception4.StatusCode);
        }

        [Fact]
        public async Task GivenAnInvalidStudySeriesOrInstanceUID_WhenDeletingMetadata_NotFoundDataStoreExceptionIsThrown()
        {
            DataStoreException exception1 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.DeleteStudyAsync(Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception1.StatusCode);

            DataStoreException exception2 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception2.StatusCode);

            DataStoreException exception3 = await Assert.ThrowsAsync<DataStoreException>(
                () => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Equal((int)HttpStatusCode.NotFound, exception3.StatusCode);
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenAddingInstanceMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(null));

            // Missing Study Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(new DicomDataset()
            {
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
            }));

            // Missing Series Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
            }));

            // Missing SOP Instance UID
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
            }));
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenFetchingInstancesOrMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataWithAllOptionalAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(Guid.NewGuid().ToString(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(Guid.NewGuid().ToString(), new string('a', 65)));
        }

        [Fact]
        public async Task GivenInvalidArguments_WhenDeletingMetadata_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteStudyAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteStudyAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteStudyAsync(new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteSeriesAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteSeriesAsync(Guid.NewGuid().ToString(), new string('a', 65)));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(string.Empty, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(new string('a', 65), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), new string('a', 65), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomMetadataStore.DeleteInstanceAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new string('a', 65)));
        }

        [Fact]
        public async Task GivenAStudy_WhenAddingAndDeletingMetadataInParallel_IsAddedAndRetriedCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            IList<DicomDataset> study = CreateStudyDataset(studyInstanceUID, DateTime.UtcNow, numberOfInstancesInSeries: 2).ToList();
            IList<DicomInstance> expectedInstances = study.Select(x => DicomInstance.Create(x)).ToList();

            await Task.WhenAll(study.Select(x => _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(x)));

            // Validate all instances added correctly.
            IEnumerable<DicomInstance> instances = await _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Equal(study.Count, instances.Count());
            foreach (DicomInstance instance in instances)
            {
                Assert.True(expectedInstances.Contains(instance));
            }

            await Task.WhenAll(expectedInstances.Select(x => _dicomMetadataStore.DeleteInstanceAsync(x.StudyInstanceUID, x.SeriesInstanceUID, x.SopInstanceUID)));

            DataStoreException exception1 = await Assert.ThrowsAsync<DataStoreException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID));
            Assert.Equal((int)HttpStatusCode.NotFound, exception1.StatusCode);
        }

        [Fact]
        public async Task GivenAStudy_WhenAddedMetadataStore_CanRetrieveTheCorrectInstancesInTheStudyAndSeries()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            IList<DicomDataset> study = CreateStudyDataset(studyInstanceUID, DateTime.UtcNow).ToList();
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
            DicomDataset instance1 = CreateInstanceDataset(studyInstanceUID, seriesInstanceUID, 1);
            instance1.AddOrUpdate(DicomTag.PatientName, "Mr^Doe^John");
            instance1.AddOrUpdate(DicomTag.StudyDate, DateTime.UtcNow.AddDays(-3));
            instance1.AddOrUpdate(DicomTag.StudyTime, DateTime.UtcNow.AddDays(-3));
            instance1.Add(DicomTag.ReferringPhysicianName, "Bob");

            DicomDataset instance2 = CreateInstanceDataset(studyInstanceUID, seriesInstanceUID, 2);
            instance2.AddOrUpdate(DicomTag.PatientName, "Mr^User^Test");
            instance2.AddOrUpdate(DicomTag.StudyDate, DateTime.UtcNow.AddDays(-10));
            instance2.AddOrUpdate(DicomTag.StudyTime, DateTime.UtcNow.AddDays(-10));

            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance1);
            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance2);

            DicomDataset studyMetadata1 = await _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID);
            Assert.Equal(studyInstanceUID, studyMetadata1.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(1, studyMetadata1.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedSeries));
            Assert.Equal(2, studyMetadata1.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedInstances));
            Assert.Equal(instance1.GetSingleValue<string>(DicomTag.PatientName), studyMetadata1.GetSingleValue<string>(DicomTag.PatientName));
            Assert.Equal(instance1.GetSingleValue<DateTime>(DicomTag.StudyDate), studyMetadata1.GetSingleValue<DateTime>(DicomTag.StudyDate));
            Assert.Equal(instance1.GetSingleValue<DateTime>(DicomTag.StudyTime), studyMetadata1.GetSingleValue<DateTime>(DicomTag.StudyTime));
            Assert.Equal(instance1.GetSingleValue<string>(DicomTag.ReferringPhysicianName), studyMetadata1.GetSingleValue<string>(DicomTag.ReferringPhysicianName));

            await _dicomMetadataStore.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, instance1.GetSingleValue<string>(DicomTag.SOPInstanceUID));

            DicomDataset studyMetadata2 = await _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID);
            Assert.Equal(studyInstanceUID, studyMetadata2.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(1, studyMetadata2.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedSeries));
            Assert.Equal(1, studyMetadata2.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedInstances));
            Assert.Equal(instance2.GetSingleValue<string>(DicomTag.PatientName), studyMetadata2.GetSingleValue<string>(DicomTag.PatientName));
            Assert.Equal(instance2.GetSingleValue<DateTime>(DicomTag.StudyDate), studyMetadata2.GetSingleValue<DateTime>(DicomTag.StudyDate));
            Assert.Equal(instance2.GetSingleValue<DateTime>(DicomTag.StudyTime), studyMetadata2.GetSingleValue<DateTime>(DicomTag.StudyTime));
            Assert.False(studyMetadata2.Contains(DicomTag.ReferringPhysicianName));
        }

        [Fact]
        public async Task GivenAStudy_WhenStudyAddedToMetadataStore_CanRetrieveTheStudyMetadataCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            int numberOfSeriesInStudy = 2;
            int numberOfInstancesPerSeries = 5;
            IList<DicomDataset> study = CreateStudyDataset(studyInstanceUID, DateTime.UtcNow, numberOfSeriesInStudy, numberOfInstancesPerSeries).ToList();

            foreach (DicomDataset dataset in study)
            {
                await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(dataset);
            }

            DicomDataset studyMetadata = await _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID, new HashSet<DicomAttributeId>() { new DicomAttributeId(DicomTag.PatientName) });
            Assert.NotNull(studyMetadata);
            Assert.Equal(studyInstanceUID, studyMetadata.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(numberOfSeriesInStudy, studyMetadata.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedSeries));
            Assert.Equal(numberOfSeriesInStudy * numberOfInstancesPerSeries, studyMetadata.GetSingleValue<int>(DicomTag.NumberOfStudyRelatedInstances));
            Assert.Equal(study[0].GetSingleValue<string>(DicomTag.PatientName), studyMetadata.GetSingleValue<string>(DicomTag.PatientName));
            Assert.Equal(study[0].GetSingleValue<DateTime>(DicomTag.StudyDate), studyMetadata.GetSingleValue<DateTime>(DicomTag.StudyDate));
            Assert.Equal(study[0].GetSingleValue<DateTime>(DicomTag.StudyTime), studyMetadata.GetSingleValue<DateTime>(DicomTag.StudyTime));
        }

        [Fact]
        public async Task GivenAStudy_WhenStudyAddedToMetadataStore_CanRetrieveTheSeriesMetadataCorrectly()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string[] seriesInstanceUIDs = new string[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var seriesDatasets = new IList<DicomDataset>[2]
            {
                CreateSeriesDataset(
                    studyInstanceUID,
                    seriesInstanceUIDs[0],
                    numberOfInstancesInSeries: 3,
                    seriesDateTime: DateTime.UtcNow.AddDays(3),
                    performedProcedureStepStartDateTime: DateTime.UtcNow.AddDays(11)).ToList(),
                CreateSeriesDataset(
                    studyInstanceUID,
                    seriesInstanceUIDs[1],
                    numberOfInstancesInSeries: 3,
                    seriesDateTime: DateTime.UtcNow.AddDays(3),
                    performedProcedureStepStartDateTime: DateTime.UtcNow.AddDays(11)).ToList(),
            };

            foreach (IList<DicomDataset> datasets in seriesDatasets)
            {
                foreach (DicomDataset dataset in datasets)
                {
                    await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(dataset);
                }
            }

            for (int i = 0; i < seriesDatasets.Length; i++)
            {
                DicomDataset referenceDataset = seriesDatasets[i].First();
                string seriesInstanceUID = referenceDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);

                DicomDataset seriesMetadata = await _dicomMetadataStore.GetSeriesDicomMetadataWithAllOptionalAsync(studyInstanceUID, seriesInstanceUID);
                Assert.NotNull(seriesMetadata);

                // Required Attributes
                Assert.Equal(seriesInstanceUID, seriesMetadata.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.SpecificCharacterSet), seriesMetadata.GetSingleValue<string>(DicomTag.SpecificCharacterSet));
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.Modality), seriesMetadata.GetSingleValue<string>(DicomTag.Modality));
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.TimezoneOffsetFromUTC), seriesMetadata.GetSingleValue<string>(DicomTag.TimezoneOffsetFromUTC));
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.SeriesDescription), seriesMetadata.GetSingleValue<string>(DicomTag.SeriesDescription));
                Assert.Equal(referenceDataset.GetSingleValue<DateTime>(DicomTag.PerformedProcedureStepStartDate), seriesMetadata.GetSingleValue<DateTime>(DicomTag.PerformedProcedureStepStartDate));
                Assert.Equal(referenceDataset.GetSingleValue<DateTime>(DicomTag.PerformedProcedureStepStartTime), seriesMetadata.GetSingleValue<DateTime>(DicomTag.PerformedProcedureStepStartTime));
                Assert.Equal(referenceDataset.GetSequence(DicomTag.RequestAttributesSequence), seriesMetadata.GetSequence(DicomTag.RequestAttributesSequence));

                // Optional Attributes
                Assert.Equal(referenceDataset.GetSingleValue<int>(DicomTag.SeriesNumber), seriesMetadata.GetSingleValue<int>(DicomTag.SeriesNumber));
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.Laterality), seriesMetadata.GetSingleValue<string>(DicomTag.Laterality));
                Assert.Equal(referenceDataset.GetSingleValue<DateTime>(DicomTag.SeriesDate), seriesMetadata.GetSingleValue<DateTime>(DicomTag.SeriesDate));
                Assert.Equal(referenceDataset.GetSingleValue<DateTime>(DicomTag.SeriesTime), seriesMetadata.GetSingleValue<DateTime>(DicomTag.SeriesTime));
            }
        }

        [Fact]
        public async Task GivenAStudyWithOneInstance_WhenDeletingInstanceMetadata_StudyAndSeriesIsDeleted()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();
            DicomDataset instance1 = CreateInstanceDataset(studyInstanceUID, seriesInstanceUID, 1);
            var dicomInstance = DicomInstance.Create(instance1);

            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance1);

            IEnumerable<DicomInstance> instancesInSeries = await _dicomMetadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Single(instancesInSeries);
            Assert.Equal(dicomInstance, instancesInSeries.First());

            IEnumerable<DicomInstance> instancesInStudy = await _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Single(instancesInStudy);
            Assert.Equal(dicomInstance, instancesInStudy.First());

            await _dicomMetadataStore.DeleteInstanceAsync(studyInstanceUID, seriesInstanceUID, dicomInstance.SopInstanceUID);
            await ValidateStudySeriesMetadataIsDeletedAsync(studyInstanceUID, seriesInstanceUID);
        }

        [Fact]
        public async Task GivenAStudyWithOneInstance_WhenDeletingSeriesMetadata_StudyIsDeleted()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();
            DicomDataset instance1 = CreateInstanceDataset(studyInstanceUID, seriesInstanceUID, 1);
            var dicomInstance = DicomInstance.Create(instance1);

            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance1);

            IEnumerable<DicomInstance> instancesInSeries = await _dicomMetadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Single(instancesInSeries);
            Assert.Equal(dicomInstance, instancesInSeries.First());

            await _dicomMetadataStore.DeleteSeriesAsync(studyInstanceUID, seriesInstanceUID);
            await ValidateStudySeriesMetadataIsDeletedAsync(studyInstanceUID, seriesInstanceUID);
        }

        [Fact]
        public async Task GivenAStudy_WhenDeletingStudyMetadata_StudyIsDeleted()
        {
            string studyInstanceUID = Guid.NewGuid().ToString();
            string seriesInstanceUID = Guid.NewGuid().ToString();
            DicomDataset instance1 = CreateInstanceDataset(studyInstanceUID, seriesInstanceUID, 1);
            var dicomInstance = DicomInstance.Create(instance1);

            await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(instance1);

            IEnumerable<DicomInstance> instancesInStudy = await _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID);
            Assert.Single(instancesInStudy);
            Assert.Equal(dicomInstance, instancesInStudy.First());

            await _dicomMetadataStore.DeleteStudyAsync(studyInstanceUID);
            await ValidateStudySeriesMetadataIsDeletedAsync(studyInstanceUID, seriesInstanceUID);
        }

        private async Task ValidateStudySeriesMetadataIsDeletedAsync(string studyInstanceUID, string seriesInstanceUID)
        {
            DataStoreException exception1 = await Assert.ThrowsAsync<DataStoreException>(() => _dicomMetadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID));
            Assert.Equal((int)HttpStatusCode.NotFound, exception1.StatusCode);

            DataStoreException exception2 = await Assert.ThrowsAsync<DataStoreException>(() => _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID));
            Assert.Equal((int)HttpStatusCode.NotFound, exception2.StatusCode);

            DataStoreException exception3 = await Assert.ThrowsAsync<DataStoreException>(() => _dicomMetadataStore.GetSeriesDicomMetadataAsync(studyInstanceUID, seriesInstanceUID));
            Assert.Equal((int)HttpStatusCode.NotFound, exception1.StatusCode);

            DataStoreException exception4 = await Assert.ThrowsAsync<DataStoreException>(() => _dicomMetadataStore.GetStudyDicomMetadataAsync(studyInstanceUID));
            Assert.Equal((int)HttpStatusCode.NotFound, exception2.StatusCode);
        }

        private IEnumerable<DicomDataset> CreateStudyDataset(
            string studyInstanceUID,
            DateTime studyDateTime,
            int numberOfSeriesInStudy = 2,
            int numberOfInstancesInSeries = 5,
            string accessionNumber = "1",
            string referringPhysicianName = "Mr^User^Test",
            string patientName = "Mrs^Doe&John",
            string specificCharacterSet = "ISO_IR 192",
            string timezoneOffsetFromUTC = "+0000")
        {
            for (int series = 0; series < numberOfSeriesInStudy; series++)
            {
                foreach (DicomDataset dataset in CreateSeriesDataset(
                                                    studyInstanceUID,
                                                    seriesInstanceUID: Guid.NewGuid().ToString(),
                                                    numberOfInstancesInSeries: numberOfInstancesInSeries,
                                                    seriesDateTime: DateTime.UtcNow,
                                                    performedProcedureStepStartDateTime: DateTime.UtcNow,
                                                    specificCharacterSet: specificCharacterSet,
                                                    timezoneOffsetFromUTC: timezoneOffsetFromUTC))
                {
                    dataset.Add(DicomTag.StudyDate, studyDateTime);
                    dataset.Add(DicomTag.StudyTime, studyDateTime);
                    dataset.Add(DicomTag.AccessionNumber, accessionNumber);
                    dataset.Add(DicomTag.ReferringPhysicianName, referringPhysicianName);
                    dataset.Add(DicomTag.PatientName, patientName);
                    dataset.Add(DicomTag.PatientID, "5");
                    dataset.Add(DicomTag.PatientBirthDate, new DateTime(1990, 2, 15));
                    dataset.Add(DicomTag.PatientSex, "U");
                    dataset.Add(DicomTag.StudyID, "8");

                    yield return dataset;
                }
            }
        }

        private IEnumerable<DicomDataset> CreateSeriesDataset(
            string studyInstanceUID,
            string seriesInstanceUID,
            int numberOfInstancesInSeries,
            DateTime seriesDateTime,
            DateTime performedProcedureStepStartDateTime,
            int seriesNumber = 1,
            string laterality = "R",
            string modality = "CT",
            string seriesDescription = "Test Series",
            string specificCharacterSet = "ISO_IR 192",
            string timezoneOffsetFromUTC = "+0000")
        {
            for (var i = 0; i < numberOfInstancesInSeries; i++)
            {
                DicomDataset instanceDataset = CreateInstanceDataset(
                    studyInstanceUID,
                    seriesInstanceUID,
                    i + 1,
                    specificCharacterSet: specificCharacterSet,
                    timezoneOffsetFromUTC: timezoneOffsetFromUTC);

                instanceDataset.Add(DicomTag.Modality, modality);
                instanceDataset.Add(DicomTag.SeriesDescription, seriesDescription);
                instanceDataset.Add(DicomTag.PerformedProcedureStepStartDate, performedProcedureStepStartDateTime);
                instanceDataset.Add(DicomTag.PerformedProcedureStepStartTime, performedProcedureStepStartDateTime);
                instanceDataset.Add(
                    new DicomSequence(
                        DicomTag.RequestAttributesSequence,
                        new DicomDataset()
                        {
                            { DicomTag.RequestedProcedureID, "8" },
                            { DicomTag.ScheduledProcedureStepID, "9" },
                        },
                        new DicomDataset()
                        {
                            { DicomTag.RequestedProcedureID, "10" },
                            { DicomTag.ScheduledProcedureStepID, "11" },
                        }));

                instanceDataset.Add(DicomTag.SeriesNumber, seriesNumber);
                instanceDataset.Add(DicomTag.Laterality, laterality);
                instanceDataset.Add(DicomTag.SeriesDate, seriesDateTime);
                instanceDataset.Add(DicomTag.SeriesTime, seriesDateTime);

                yield return instanceDataset;
            }
        }

        private DicomDataset CreateInstanceDataset(
            string studyInstanceUID,
            string seriesInstanceUID,
            int instanceNumber,
            ushort rows = 256,
            ushort columns = 512,
            ushort bitsAllocated = 16,
            int numberOfFrames = 1,
            string specificCharacterSet = "ISO_IR 192",
            string timezoneOffsetFromUTC = "+0000")
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.TimezoneOffsetFromUTC, timezoneOffsetFromUTC },
                { DicomTag.InstanceNumber, instanceNumber },
                { DicomTag.Rows, rows },
                { DicomTag.Columns, columns },
                { DicomTag.BitsAllocated, bitsAllocated },
                { DicomTag.NumberOfFrames, numberOfFrames },
                { DicomTag.SpecificCharacterSet, specificCharacterSet },
            };
        }
    }
}
