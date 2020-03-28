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
    public class RetrieveDicomResourceRequestValidatorTests
    {
        [Theory]
        [InlineData("*-")]
        [InlineData("invalid")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenIncorrectTransferSyntax_OnValidationOfRetrieveRequest_ErrorReturned(string transferSyntax)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Requested Representation'.";
            var request = new RetrieveDicomResourceRequest(transferSyntax, TestUidGenerator.Generate());

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
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
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
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                frames: frames,
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData("1", "1", "2")]
        [InlineData("1", "2", "1")]
        [InlineData("1", "2", "2")]
        public void GivenRepeatedIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(
            string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            const string expectedErrorMessage = "The specified condition was not met for ''.";
            var request = new RetrieveDicomResourceRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);

            request = new RetrieveDicomResourceRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                frames: new int[] { 1 },
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData("()")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa65")]
        public void GivenInvalidIdentifiers_OnValidationOfRetrieveRequest_ErrorReturned(string invalidIdentifier)
        {
            var request = new RetrieveDicomResourceRequest(
                studyInstanceUid: invalidIdentifier,
                requestedTransferSyntax: "*");
            ValidateHasError(request, "Study Instance Uid");

            request = new RetrieveDicomResourceRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: invalidIdentifier,
                requestedTransferSyntax: "*");
            ValidateHasError(request, "Series Instance Uid");

            request = new RetrieveDicomResourceRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: invalidIdentifier,
                requestedTransferSyntax: "*");
            ValidateHasError(request, "Sop Instance Uid");

            request = new RetrieveDicomResourceRequest(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: invalidIdentifier,
                frames: new[] { 1 },
                requestedTransferSyntax: "*");
            ValidateHasError(request, "Sop Instance Uid");
        }

        private static void ValidateHasError(RetrieveDicomResourceRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new RetrieveDicomResourcesRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
