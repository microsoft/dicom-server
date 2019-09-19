// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FluentValidation.Results;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Delete
{
    public class DeleteDicomResourcesRequestValidatorTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(string invalidIdentifier)
        {
            var request = new DeleteDicomResourcesRequest(invalidIdentifier);
            ValidateHasError(request, "'Study Instance UID' is not in the correct format.");

            request = new DeleteDicomResourcesRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: invalidIdentifier);
            ValidateHasError(request, "'Series Instance UID' is not in the correct format.");

            request = new DeleteDicomResourcesRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: Guid.NewGuid().ToString(),
                sopInstanceUID: invalidIdentifier);
            ValidateHasError(request, "'Sop Instance UID' is not in the correct format.");
        }

        [Theory]
        [InlineData("1", "1", "2", "Study Instance UID")]
        [InlineData("1", "2", "1", "Study Instance UID")]
        [InlineData("1", "2", "2", "Series Instance UID")]
        public void GivenRepeatedIdentifiers_OnValidationOfRetrieveMetadataRequest_ErrorReturned(
            string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, string parameterError)
        {
            string expectedErrorMessage = $"The specified condition was not met for '{parameterError}'.";

            var request = new DeleteDicomResourcesRequest(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            ValidateHasError(request, expectedErrorMessage);

            // Always use the same identifier for series request
            request = new DeleteDicomResourcesRequest(studyInstanceUID, studyInstanceUID);
            ValidateHasError(request, $"The specified condition was not met for 'Study Instance UID'.");
        }

        private static void ValidateHasError(DeleteDicomResourcesRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new DeleteDicomResourcesRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
