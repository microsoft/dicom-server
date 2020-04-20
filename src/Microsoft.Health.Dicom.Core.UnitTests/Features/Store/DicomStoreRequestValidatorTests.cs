// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomStoreRequestValidatorTests
    {
        [Fact]
        public void GivenNullRequestBody_WhenValidated_ThenDicomBadRequestExceptionShouldBeThrown()
        {
            DicomStoreRequest request = new DicomStoreRequest(null, "application/dicom");

            Assert.Throws<DicomBadRequestException>(() => DicomStoreRequestValidator.ValidateRequest(request));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1.2.3")]
        public void GivenAValidStudyInstanceId_WhenValidated_ThenItShouldSucceed(string studyInstanceUid)
        {
            DicomStoreRequest request = new DicomStoreRequest(Stream.Null, "application/dicom", studyInstanceUid);

            DicomStoreRequestValidator.ValidateRequest(request);
        }

        [Theory]
        [InlineData("1.01.2")]
        [InlineData("invalid")]
        public void GivenAnInvalidStudyInstanceUid_WhenValidated_ThenDicomBadRequestExceptionShouldBeThrown(string studyInstanceUid)
        {
            DicomStoreRequest request = new DicomStoreRequest(Stream.Null, "application/dicom", studyInstanceUid);

            Assert.Throws<DicomInvalidIdentifierException>(() => DicomStoreRequestValidator.ValidateRequest(request));
        }
    }
}
