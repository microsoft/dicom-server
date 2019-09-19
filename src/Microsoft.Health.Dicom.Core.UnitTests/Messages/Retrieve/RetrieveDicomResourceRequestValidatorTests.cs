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
    public class RetrieveDicomResourceRequestValidatorTests
    {
        [Theory]
        [InlineData("*-")]
        [InlineData("invalid")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenIncorrectTransferSyntax_OnValidationOfRetrieveRequest_ErrorReturned(string transferSyntax)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Requested Transfer Syntax'.";
            var request = new RetrieveDicomResourceRequest(transferSyntax, Guid.NewGuid().ToString());

            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-234)]
        public void GivenInvalidFrameNumber_OnValidationOfRetrieveRequest_ErrorReturned(int frame)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Frames'.";
            var request = new RetrieveDicomResourceRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: Guid.NewGuid().ToString(),
                sopInstanceUID: Guid.NewGuid().ToString(),
                frames: new[] { frame },
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new int[0])]
        public void GivenNoFrames_OnValidationOfRetrieveRequest_ErrorReturned(int[] frames)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Frames'.";
            var request = new RetrieveDicomResourceRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: Guid.NewGuid().ToString(),
                sopInstanceUID: Guid.NewGuid().ToString(),
                frames: frames,
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public void GivenRepeatedIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(
            string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            const string expectedErrorMessage = "The specified condition was not met for ''.";
            var request = new RetrieveDicomResourceRequest(
                studyInstanceUID: studyInstanceUID,
                seriesInstanceUID: seriesInstanceUID,
                sopInstanceUID: sopInstanceUID,
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);

            request = new RetrieveDicomResourceRequest(
                studyInstanceUID: studyInstanceUID,
                seriesInstanceUID: seriesInstanceUID,
                sopInstanceUID: sopInstanceUID,
                frames: new int[] { 1 },
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(string invalidIdentifier)
        {
            var request = new RetrieveDicomResourceRequest(
                studyInstanceUID: invalidIdentifier,
                requestedTransferSyntax: "*");
            ValidateHasError(request, "'Study Instance UID' is not in the correct format.");

            request = new RetrieveDicomResourceRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: invalidIdentifier,
                requestedTransferSyntax: "*");
            ValidateHasError(request, "'Series Instance UID' is not in the correct format.");

            request = new RetrieveDicomResourceRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: Guid.NewGuid().ToString(),
                sopInstanceUID: invalidIdentifier,
                requestedTransferSyntax: "*");
            ValidateHasError(request, "'Sop Instance UID' is not in the correct format.");

            request = new RetrieveDicomResourceRequest(
                studyInstanceUID: Guid.NewGuid().ToString(),
                seriesInstanceUID: Guid.NewGuid().ToString(),
                sopInstanceUID: invalidIdentifier,
                frames: new[] { 1 },
                requestedTransferSyntax: "*");
            ValidateHasError(request, "'Sop Instance UID' is not in the correct format.");
        }

        private static void ValidateHasError(RetrieveDicomResourceRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new RetrieveDicomResourcesRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
