// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FluentValidation.Results;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class RetrieveDicomMetadataRequestValidatorTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(string invalidIdentifier)
        {
            var request = new RetrieveDicomMetadataRequest(invalidIdentifier);
            ValidateHasError(request, "'Study Instance UID' is not in the correct format.");

            request = new RetrieveDicomMetadataRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: invalidIdentifier);
            ValidateHasError(request, "'Series Instance UID' is not in the correct format.");

            request = new RetrieveDicomMetadataRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: Guid.NewGuid().ToString(),
                sopInstanceUID: invalidIdentifier);
            ValidateHasError(request, "'Sop Instance UID' is not in the correct format.");
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
            Assert.Equal(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
