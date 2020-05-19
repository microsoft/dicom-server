// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomDatasetMinimumRequirementValidatorTests
    {
        private const ushort ValidationFailedFailureCode = 43264;
        private const ushort MismatchStudyInstanceUidFailureCode = 43265;

        private readonly DicomDatasetMinimumRequirementValidator _dicomDatasetMinimumRequirementValidator = new DicomDatasetMinimumRequirementValidator();

        private readonly DicomDataset _dicomDataset = Samples.CreateRandomInstanceDataset();

        [Fact]
        public void GivenAValidDicomDataset_WhenValidated_ThenItShouldSucceed()
        {
            _dicomDatasetMinimumRequirementValidator.Validate(_dicomDataset, requiredStudyInstanceUid: null);
        }

        [Fact]
        public void GivenAValidDicomDatasetThatMatchesTheRequiredStudyInstanceUid_WhenValidated_ThenItShouldSucceed()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            _dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);

            _dicomDatasetMinimumRequirementValidator.Validate(
                _dicomDataset,
                studyInstanceUid);
        }

        [Theory]
        [InlineData("1.01")]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        [InlineData("1.2.826.0.1.3680043.8.1055.1.20111102150758591.96842950.07877442")]
        public void GivenADicomDatasetWithInvalidStudyInstanceUid_WhenValidated_ThenInvalidIdentifierExceptionIsThrown(string studyInstanceUid)
        {
            // Disabled autovalidation on this dataset so that invalid UId's can be added.
#pragma warning disable CS0618 // Type or member is obsolete
            _dicomDataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete

            _dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        [Theory]
        [InlineData("1.01")]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        [InlineData("1.2.826.0.1.3680043.8.1055.1.20111102150758591.96842950.07877442")]
        public void GivenADicomDatasetWithInvalidSeriesInstanceUid_WhenValidated_ThenInvalidIdentifierExceptionIsThrown(string seriesInstanceUid)
        {
            // Disabled autovalidation on this dataset so that invalid UId's can be added.
#pragma warning disable CS0618 // Type or member is obsolete
            _dicomDataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete

            _dicomDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesInstanceUid);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        [Theory]
        [InlineData("1.01")]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        [InlineData("1.2.826.0.1.3680043.8.1055.1.20111102150758591.96842950.07877442")]
        public void GivenADicomDatasetWithInvalidSopInstanceUid_WhenValidated_ThenInvalidIdentifierExceptionIsThrown(string sopInstanceUid)
        {
            // Disabled autovalidation on this dataset so that invalid UId's can be added.
#pragma warning disable CS0618 // Type or member is obsolete
            _dicomDataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete

            _dicomDataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        [Theory]
        [InlineData("1.01")]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        [InlineData("1.2.826.0.1.3680043.8.1055.1.20111102150758591.96842950.07877442")]
        public void GivenADicomDatasetWithInvalidSopClassUid_WhenValidated_ThenInvalidIdentifierExceptionIsThrown(string sopClassUid)
        {
            // Disabled autovalidation on this dataset so that invalid UId's can be added.
#pragma warning disable CS0618 // Type or member is obsolete
            _dicomDataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete

            _dicomDataset.AddOrUpdate(DicomTag.SOPClassUID, sopClassUid);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        public static IEnumerable<object[]> GetDicomTagsToRemove()
        {
            return new List<object[]>
            {
                new[] { DicomTag.PatientID.ToString() },
                new[] { DicomTag.StudyInstanceUID.ToString() },
                new[] { DicomTag.SeriesInstanceUID.ToString() },
                new[] { DicomTag.SOPInstanceUID.ToString() },
                new[] { DicomTag.SOPClassUID.ToString() },
            };
        }

        [Theory]
        [MemberData(nameof(GetDicomTagsToRemove))]
        public void GivenAMissingTag_WhenValidated_ThenDicomDatasetMinimumRequirementExceptionShouldBeThrown(string dicomTagInString)
        {
            DicomTag dicomTag = DicomTag.Parse(dicomTagInString);

            _dicomDataset.Remove(dicomTag);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        public static IEnumerable<object[]> GetDuplicatedDicomIdentifierValues()
        {
            return new List<object[]>
            {
                new[] { DicomTag.StudyInstanceUID.ToString(), DicomTag.SeriesInstanceUID.ToString() },
                new[] { DicomTag.StudyInstanceUID.ToString(), DicomTag.SOPInstanceUID.ToString() },
                new[] { DicomTag.SeriesInstanceUID.ToString(), DicomTag.SOPInstanceUID.ToString() },
            };
        }

        [Theory]
        [MemberData(nameof(GetDuplicatedDicomIdentifierValues))]
        public void GivenDuplicatedIdentifiers_WhenValidated_ThenDicomDatasetMinimumRequirementExceptionShouldBeThrown(string firstDicomTagInString, string secondDicomTagInString)
        {
            DicomTag firstDicomTag = DicomTag.Parse(firstDicomTagInString);
            DicomTag secondDicomTag = DicomTag.Parse(secondDicomTagInString);

            string value = _dicomDataset.GetSingleValue<string>(firstDicomTag);
            _dicomDataset.AddOrUpdate(secondDicomTag, value);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        [Fact]
        public void GivenStudyInstanceUidDoesNotMatchWithRequiredStudyInstanceUid_WhenValidated_ThenDicomDatasetMinimumRequirementExceptionShouldBeThrown()
        {
            string requiredStudyInstanceUid = null;
            string studyInstanceUid = _dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

            do
            {
                requiredStudyInstanceUid = TestUidGenerator.Generate();
            }
            while (string.Equals(requiredStudyInstanceUid, studyInstanceUid, System.StringComparison.InvariantCultureIgnoreCase));

            ExecuteAndValidateException(MismatchStudyInstanceUidFailureCode, requiredStudyInstanceUid);
        }

        private void ExecuteAndValidateException(ushort failureCode, string requiredStudyInstanceUid = null)
        {
            var exception = Assert.Throws<DatasetValidationException>(
                () => _dicomDatasetMinimumRequirementValidator.Validate(_dicomDataset, requiredStudyInstanceUid));

            Assert.Equal(failureCode, exception.FailureCode);
        }
    }
}
