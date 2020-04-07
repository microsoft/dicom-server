// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation.Results;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class DicomRetrieveMetadataRequestValidatorTests
    {
        [Theory]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidStudyInstanceUid_WhenRetrieveMetadataRequestIsValidated_ThenErrorIsReturned(string invalidIdentifier)
        {
            var request = new DicomRetrieveMetadataRequest(studyInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Study Instance Uid");
        }

        [Theory]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidSeriesInstanceUid_WhenRetrieveMetadataRequestIsValidated_ThenErrorIsReturned(string invalidIdentifier)
        {
            var request = new DicomRetrieveMetadataRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Series Instance Uid");
        }

        [Theory]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidSopInstanceUid_WhenRetrieveMetadataRequestIsValidated_ThenErrorIsReturned(string invalidIdentifier)
        {
            var request = new DicomRetrieveMetadataRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Sop Instance Uid");
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public void GivenRepeatedIdentifiers_WhenRetrieveMetadataRequestIsValidated_ThenErrorIsReturned(
            string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            const string expectedErrorMessage = "The specified condition was not met for ''.";

            var request = new DicomRetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            ValidateHasError(request, expectedErrorMessage);

            // Always use the same identifier for series request
            request = new DicomRetrieveMetadataRequest(studyInstanceUid, studyInstanceUid);
            ValidateHasError(request, expectedErrorMessage);
        }

        private static void ValidateHasError(DicomRetrieveMetadataRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new DicomRetrieveMetadataRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
