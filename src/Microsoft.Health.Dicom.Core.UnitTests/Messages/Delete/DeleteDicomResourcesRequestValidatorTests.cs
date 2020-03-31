// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation.Results;
using Microsoft.Health.Dicom.Core.Messages.Delete;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Delete
{
    public class DeleteDicomResourcesRequestValidatorTests
    {
        [Theory]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(string invalidIdentifier)
        {
            var request = new DeleteDicomResourcesRequest(invalidIdentifier);
            ValidateHasError(request, "Study Instance Uid");

            request = new DeleteDicomResourcesRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Series Instance Uid");

            request = new DeleteDicomResourcesRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Sop Instance Uid");
        }

        [Theory]
        [InlineData("1", "1", "2", "Study Instance Uid")]
        [InlineData("1", "2", "1", "Study Instance Uid")]
        [InlineData("1", "2", "2", "Series Instance Uid")]
        public void GivenRepeatedIdentifiers_OnValidationOfRetrieveMetadataRequest_ErrorReturned(
            string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, string parameterError)
        {
            string expectedErrorMessage = $"The specified condition was not met for '{parameterError}'.";

            var request = new DeleteDicomResourcesRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            ValidateHasError(request, expectedErrorMessage);

            // Always use the same identifier for series request
            request = new DeleteDicomResourcesRequest(studyInstanceUid, studyInstanceUid);
            ValidateHasError(request, $"The specified condition was not met for 'Study Instance Uid'.");
        }

        private static void ValidateHasError(DeleteDicomResourcesRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new DeleteDicomResourcesRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
