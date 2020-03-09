// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Persistence
{
    public class DicomAttributeIdExtensionsTests
    {
        [Fact]
        public void GivenDatasetAndInvalidParameters_WhenAddingAttributeValues_ArgumentExceptionIsThrown()
        {
            var dicomDataset = new DicomDataset();
            var testAttributeId = new DicomAttributeId(DicomTag.ReferencedStudySequence, DicomTag.StudyInstanceUID);
            var testDicomItem = new DicomLongString(DicomTag.StudyInstanceUID, TestUidGenerator.Generate());
            Assert.Throws<ArgumentNullException>(() => dicomDataset.Add((DicomAttributeId)null, testDicomItem));
            Assert.Throws<ArgumentNullException>(() => dicomDataset.Add(testAttributeId, null));
            Assert.Throws<ArgumentNullException>(() => DicomAttributeIdExtensions.Add(null, testAttributeId, testDicomItem));
            Assert.Throws<ArgumentException>(() => dicomDataset.Add(testAttributeId, new DicomLongString(DicomTag.SeriesInstanceUID, TestUidGenerator.Generate())));
        }

        [Fact]
        public void GivenDicomAttributeIdWithSequenceElements_WhenAddingDicomItemToDataset_IsAddedCorrectly()
        {
            var dicomDataset = new DicomDataset();
            var testStudyId = TestUidGenerator.Generate();
            dicomDataset.Add(
                new DicomAttributeId(DicomTag.ReferencedStudySequence, DicomTag.StudyInstanceUID),
                new DicomLongString(DicomTag.StudyInstanceUID, testStudyId));

            Assert.True(dicomDataset.TryGetSequence(DicomTag.ReferencedStudySequence, out DicomSequence referencedStudySequence));
            Assert.Single(referencedStudySequence.Items);
            Assert.Single(referencedStudySequence.Items[0]);
            Assert.Equal(testStudyId, referencedStudySequence.Items[0].GetSingleValue<string>(DicomTag.StudyInstanceUID));
        }

        [Fact]
        public void GivenDicomAttributeIdWithSequenceElements_WhenAddingSequenceDicomItemToDataset_IsAddedCorrectly()
        {
            var dicomDataset = new DicomDataset();
            var resultSequence = new DicomSequence(
                DicomTag.ReferencedSeriesSequence,
                new DicomDataset() { { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() } },
                new DicomDataset() { { DicomTag.SeriesDescription, "TestDescription" } });
            dicomDataset.Add(
                new DicomAttributeId(DicomTag.ReferencedStudySequence, resultSequence.Tag), resultSequence);

            Assert.True(dicomDataset.TryGetSequence(DicomTag.ReferencedStudySequence, out DicomSequence referencedStudySequence));
            Assert.Single(referencedStudySequence.Items);
            Assert.Single(referencedStudySequence.Items[0]);
            Assert.Equal(resultSequence, referencedStudySequence.Items[0].GetSequence(resultSequence.Tag));
        }

        [Fact]
        public void GivenDatasetAndInvalidParameters_WhenFetchedAttributeValues_ArgumentExceptionIsThrown()
        {
            var dicomDataset = new DicomDataset();
            Assert.Throws<ArgumentNullException>(() => dicomDataset.TryGetValues((DicomAttributeId)null, out object[] values));
        }

        [Fact]
        public void GivenDataset_WhenFetchingMissingAttribute_FalseIsReturned()
        {
            var dicomDataset = new DicomDataset();
            var result = dicomDataset.TryGetValues(new DicomAttributeId(DicomTag.PatientName), out object[] values);

            Assert.False(result);
            Assert.Null(values);
        }

        [Fact]
        public void GivenDatasetWithSequence_WhenFetchingAttributeValues_IsReturnedCorrectly()
        {
            var sequence1 = new DicomSequence(
                DicomTag.ReferencedPatientSequence,
                new DicomDataset() { { DicomTag.PatientName, "Patient1" } },
                new DicomDataset() { { DicomTag.PatientName, "Patient2" } },
                new DicomDataset() { { DicomTag.PatientName, "Patient3" } });

            var fetchedValues1 = new DicomDataset() { sequence1 }.TryGetValues(
                                        new DicomAttributeId(DicomTag.ReferencedPatientSequence, DicomTag.PatientName),
                                        out object[] values1);
            Assert.True(fetchedValues1);
            Assert.Equal(3, values1.Length);
            Assert.Contains("Patient1", values1);
            Assert.Contains("Patient2", values1);
            Assert.Contains("Patient3", values1);

            var sequence2 = new DicomSequence(
                DicomTag.ReferencedStudySequence,
                new DicomDataset() { { DicomTag.StudyDate, new DateTime(2019, 6, 25) } },
                new DicomDataset() { { new DicomSequence(DicomTag.ReferencedStudySequence, new DicomDataset() { { DicomTag.StudyDate, new DateTime(2019, 6, 24) } }) } },
                new DicomDataset() { { DicomTag.StudyDate, new DateTime(2019, 6, 23) } });

            var fetchedValues2 = new DicomDataset() { sequence2 }.TryGetValues(
                                        new DicomAttributeId(DicomTag.ReferencedStudySequence, DicomTag.StudyDate),
                                        out object[] values2);
            Assert.True(fetchedValues2);
            Assert.Equal(2, values2.Length);
            Assert.Contains(new DateTime(2019, 6, 25), values2);
            Assert.Contains(new DateTime(2019, 6, 23), values2);

            var fetchedValues3 = new DicomDataset() { sequence2 }.TryGetValues(
                                        new DicomAttributeId(DicomTag.ReferencedStudySequence, DicomTag.ReferencedStudySequence, DicomTag.StudyDate),
                                        out object[] values3);
            Assert.True(fetchedValues3);
            Assert.Single(values3);
            Assert.Contains(new DateTime(2019, 6, 24), values3);
        }
    }
}
