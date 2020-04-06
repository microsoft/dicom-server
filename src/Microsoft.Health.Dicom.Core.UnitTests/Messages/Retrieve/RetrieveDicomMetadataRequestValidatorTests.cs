// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FluentValidation.Results;
using Microsoft.Health.Dicom.Core.Messages;
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
            var request = new RetrieveDicomMetadataRequest(ResourceType.Study, studyInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Study Instance Uid");

            request = new RetrieveDicomMetadataRequest(
                ResourceType.Series,
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: invalidIdentifier);
            ValidateHasError(request, "Series Instance Uid");

            request = new RetrieveDicomMetadataRequest(
                ResourceType.Instance,
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
            string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            const string expectedErrorMessage = "The specified condition was not met for ''.";

            var request = new RetrieveDicomMetadataRequest(ResourceType.Instance, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            ValidateHasError(request, expectedErrorMessage);

            // Always use the same identifier for series request
            request = new RetrieveDicomMetadataRequest(ResourceType.Series, studyInstanceUid, studyInstanceUid);
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
