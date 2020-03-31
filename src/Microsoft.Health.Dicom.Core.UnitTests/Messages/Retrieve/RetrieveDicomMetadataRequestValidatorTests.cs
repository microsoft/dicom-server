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
    public class RetrieveDicomMetadataRequestValidatorTests
    {
        [Theory]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(string invalidIdentifier)
        {
            var request = new RetrieveDicomMetadataRequest(invalidIdentifier);
            ValidateHasError(request, "Study Instance Uid");

            request = new RetrieveDicomMetadataRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Series Instance Uid");

            request = new RetrieveDicomMetadataRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Sop Instance Uid");
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public void GivenRepeatedIdentifiers_OnValidationOfRetrieveMetadataRequest_ErrorReturned(
            string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            const string expectedErrorMessage = "The specified condition was not met for ''.";

            var request = new RetrieveDicomMetadataRequest(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            ValidateHasError(request, expectedErrorMessage);

            // Always use the same identifier for series request
            request = new RetrieveDicomMetadataRequest(studyInstanceUID, studyInstanceUID);
            ValidateHasError(request, expectedErrorMessage);
        }

        private static void ValidateHasError(RetrieveDicomMetadataRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new RetrieveDicomMetadataRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
