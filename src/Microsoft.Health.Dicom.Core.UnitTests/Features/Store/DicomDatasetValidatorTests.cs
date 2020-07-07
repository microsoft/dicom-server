// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    // Run these tests exclusively serial since they change the global autovalidation
    [CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    public class DicomDatasetValidatorTests
    {
        private const ushort ValidationFailedFailureCode = 43264;
        private const ushort MismatchStudyInstanceUidFailureCode = 43265;

        private DicomDatasetValidator _dicomDatasetValidator;

        private readonly DicomDataset _dicomDataset = Samples.CreateRandomInstanceDataset();

        public DicomDatasetValidatorTests()
        {
            var featureConfiguration = Substitute.For<IOptions<FeatureConfiguration>>();
            featureConfiguration.Value.Returns(new FeatureConfiguration
            {
                EnableFullDicomItemValidation = false,
            });
            _dicomDatasetValidator = new DicomDatasetValidator(featureConfiguration);
        }

        [Fact]
        public void GivenAValidDicomDataset_WhenValidated_ThenItShouldSucceed()
        {
            _dicomDatasetValidator.Validate(_dicomDataset, requiredStudyInstanceUid: null);
        }

        [Fact]
        public void GivenAValidDicomDatasetThatMatchesTheRequiredStudyInstanceUid_WhenValidated_ThenItShouldSucceed()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            _dicomDataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);

            _dicomDatasetValidator.Validate(
                _dicomDataset,
                studyInstanceUid);
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
        public void GivenAMissingTag_WhenValidated_ThenDatasetValidationExceptionShouldBeThrown(string dicomTagInString)
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
        public void GivenDuplicatedIdentifiers_WhenValidated_ThenDatasetValidationExceptionShouldBeThrown(string firstDicomTagInString, string secondDicomTagInString)
        {
            DicomTag firstDicomTag = DicomTag.Parse(firstDicomTagInString);
            DicomTag secondDicomTag = DicomTag.Parse(secondDicomTagInString);

            string value = _dicomDataset.GetSingleValue<string>(firstDicomTag);
            _dicomDataset.AddOrUpdate(secondDicomTag, value);

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        [Fact]
        public void GivenStudyInstanceUidDoesNotMatchWithRequiredStudyInstanceUid_WhenValidated_ThenDatasetValidationExceptionShouldBeThrown()
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

        [Fact]
        public void GivenDatasetWithInvalidVrValue_WhenValidatingWithFullValidation_ThenDatasetValidationExceptionShouldBeThrown()
        {
            var featureConfiguration = Substitute.For<IOptions<FeatureConfiguration>>();
            featureConfiguration.Value.Returns(new FeatureConfiguration
            {
                EnableFullDicomItemValidation = true,
            });
            _dicomDatasetValidator = new DicomDatasetValidator(featureConfiguration);

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

            // LO VR, invalid characters
            _dicomDataset.Add(DicomTag.SeriesDescription, "CT1 abdomen\u0000");

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = true;
#pragma warning restore CS0618 // Type or member is obsolete

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        [Fact]
        public void GivenDatasetWithInvalidIndexedTagValue_WhenValidating_ThenDatasetValidationExceptionShouldBeThrown()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

            // CS VR, lower case alphabets not allowed
            _dicomDataset.Add(DicomTag.Modality, "abcd");

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = true;
#pragma warning restore CS0618 // Type or member is obsolete

            ExecuteAndValidateException(ValidationFailedFailureCode);
        }

        private void ExecuteAndValidateException(ushort failureCode, string requiredStudyInstanceUid = null)
        {
            var exception = Assert.Throws<DatasetValidationException>(
                () => _dicomDatasetValidator.Validate(_dicomDataset, requiredStudyInstanceUid));

            Assert.Equal(failureCode, exception.FailureCode);
        }
    }
}
