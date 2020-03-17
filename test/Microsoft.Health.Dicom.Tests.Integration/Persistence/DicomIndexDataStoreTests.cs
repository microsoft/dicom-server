// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomIndexDataStoreTests : IClassFixture<SqlServerDicomIndexDataStoreTestsFixture>
    {
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomIndexDataStoreTestHelper _testHelper;

        public DicomIndexDataStoreTests(SqlServerDicomIndexDataStoreTestsFixture fixture)
        {
            _dicomIndexDataStore = fixture.DicomIndexDataStore;
            _testHelper = fixture.TestHelper;
        }

        [Fact]
        public async Task GivenANonExistingDicomInstance_WhenAdded_ThenItShouldBeAdded()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            string patientId = "pid";
            string patientName = "pname";
            string referringPhysicianName = "rname";
            string studyDateTime = "2020-03-01";
            string studyDescription = "sd";
            string accessionNumber = "an";
            string modality = "M";
            string performedProcedureStepStartDate = "2020-03-02";

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            dataset.Add(DicomTag.PatientID, patientId);
            dataset.Add(DicomTag.PatientName, patientName);
            dataset.Add(DicomTag.ReferringPhysicianName, referringPhysicianName);
            dataset.Add(DicomTag.StudyDate, studyDateTime);
            dataset.Add(DicomTag.StudyDescription, studyDescription);
            dataset.Add(DicomTag.AccessionNumber, accessionNumber);
            dataset.Add(DicomTag.Modality, modality);
            dataset.Add(DicomTag.PerformedProcedureStepStartDate, performedProcedureStepStartDate);

            try
            {
                DateTime date = DateTime.UtcNow;

                await _dicomIndexDataStore.IndexInstanceAsync(dataset);

                IReadOnlyList<StudyMetadata> studyMetadataEntries = await _testHelper.GetStudyMetadataAsync(studyInstanceUid);

                Assert.Collection(
                    studyMetadataEntries,
                    entry => ValidateStudyMetadata(
                        studyInstanceUid,
                        0,
                        patientId,
                        patientName,
                        referringPhysicianName,
                        new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                        studyDescription,
                        accessionNumber,
                        entry));

                IReadOnlyList<SeriesMetadata> seriesMetadataEntries = await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid);

                Assert.Collection(
                    seriesMetadataEntries,
                    entry => ValidateSeriesMetadata(
                        seriesInstanceUid,
                        0,
                        modality,
                        new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                        entry));

                // Make sure the ID matches between the study and series metadata.
                Assert.Equal(studyMetadataEntries[0].ID, seriesMetadataEntries[0].ID);

                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                Assert.NotNull(instance);
                Assert.Equal(studyInstanceUid, instance.StudyInstanceUid);
                Assert.Equal(seriesInstanceUid, instance.SeriesInstanceUid);
                Assert.Equal(sopInstanceUid, instance.SopInstanceUid);
                Assert.True(instance.Watermark >= 0);
                Assert.Equal(DicomIndexStatus.Created, instance.Status);
                Assert.InRange(instance.LastStatusUpdatedDate, date.AddSeconds(-1), date.AddSeconds(1));
                Assert.InRange(instance.CreatedDate, date.AddSeconds(-1), date.AddSeconds(1));
            }
            finally
            {
                await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            }
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenAdded_ThenConflictExceptionShouldBeThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            dataset.Add(DicomTag.PatientID, "pid");

            try
            {
                await _dicomIndexDataStore.IndexInstanceAsync(dataset);

                await Assert.ThrowsAsync<DicomInstanceAlreadyExistsException>(() => _dicomIndexDataStore.IndexInstanceAsync(dataset));
            }
            finally
            {
                await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            }
        }

        private static void ValidateStudyMetadata(
            string expectedStudyInstanceUid,
            int expectedVersion,
            string expectedPatientId,
            string expectedPatientName,
            string expectedReferringPhysicianName,
            DateTime? expectedStudyDate,
            string expectedStudyDescription,
            string expectedAccessionNumber,
            StudyMetadata actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedStudyInstanceUid, actual.StudyInstanceUid);
            Assert.Equal(expectedVersion, actual.Version);
            Assert.Equal(expectedPatientId, actual.PatientID);
            Assert.Equal(expectedPatientName, actual.PatientName);
            Assert.Equal(expectedReferringPhysicianName, actual.ReferringPhysicianName);
            Assert.Equal(expectedStudyDate, actual.StudyDate);
            Assert.Equal(expectedStudyDescription, actual.StudyDescription);
            Assert.Equal(expectedAccessionNumber, actual.AccessionNumber);
        }

        private static void ValidateSeriesMetadata(
            string expectedSeriesInstanceUid,
            int expectedVersion,
            string expectedModality,
            DateTime? expectedPerformedProcedureStepStartDate,
            SeriesMetadata actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedSeriesInstanceUid, actual.SeriesInstanceUid);
            Assert.Equal(expectedVersion, actual.Version);
            Assert.Equal(expectedModality, actual.Modality);
            Assert.Equal(expectedPerformedProcedureStepStartDate, actual.PerformedProcedureStepStartDate);
        }
    }
}
