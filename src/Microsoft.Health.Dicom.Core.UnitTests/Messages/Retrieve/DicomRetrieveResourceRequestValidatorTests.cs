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
    public class DicomRetrieveResourceRequestValidatorTests
    {
        [Theory]
        [InlineData("*-")]
        [InlineData("invalid")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenIncorrectTransferSyntax_WhenValidatingRetrieveRequest_ThenErrorReturned(string transferSyntax)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Requested Representation'.";
            var request = new DicomRetrieveResourceRequest(transferSyntax, TestUidGenerator.Generate());

            ValidateHasError(request, expectedErrorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-234)]
        public void GivenInvalidFrameNumber_WhenValidatingRetrieveRequest_ThenErrorReturned(int frame)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Frames'.";
            var request = new DicomRetrieveResourceRequest(
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
        public void GivenNoFrames_WhenValidatingRetrieveRequest_ThenErrorReturned(int[] frames)
        {
            const string expectedErrorMessage = "The specified condition was not met for 'Frames'.";
            var request = new DicomRetrieveResourceRequest(
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
        public void GivenRepeatedIdentifiers_WhenValidatingRetrieveRequest_ThenErrorReturned(
            string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            const string expectedErrorMessage = "The specified condition was not met for ''.";
            var request = new DicomRetrieveResourceRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);

            request = new DicomRetrieveResourceRequest(
                studyInstanceUid: studyInstanceUid,
                seriesInstanceUid: seriesInstanceUid,
                sopInstanceUid: sopInstanceUid,
                frames: new int[] { 1 },
                requestedTransferSyntax: "*");
            ValidateHasError(request, expectedErrorMessage);
        }

        private static void ValidateHasError(DicomRetrieveResourceRequest request, string expectedErrorMessage)
        {
            ValidationResult result = new DicomRetrieveResourcesRequestValidator().Validate(request);
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains(expectedErrorMessage, result.Errors[0].ErrorMessage);
        }
    }
}
