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
    public class StoreRequestValidatorTests
    {
        [Fact]
        public void GivenNullRequestBody_WhenValidated_ThenBadRequestExceptionShouldBeThrown()
        {
            StoreRequest request = new StoreRequest(null, "application/dicom");

            Assert.Throws<BadRequestException>(() => StoreRequestValidator.ValidateRequest(request));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1.2.3")]
        public void GivenAValidStudyInstanceId_WhenValidated_ThenItShouldSucceed(string studyInstanceUid)
        {
            StoreRequest request = new StoreRequest(Stream.Null, "application/dicom", studyInstanceUid);

            StoreRequestValidator.ValidateRequest(request);
        }

        [Theory]
        [InlineData("1.a1.2")]
        [InlineData("invalid")]
        public void GivenAnInvalidStudyInstanceUid_WhenValidated_ThenInvalidIdentifierExceptionShouldBeThrown(string studyInstanceUid)
        {
            StoreRequest request = new StoreRequest(Stream.Null, "application/dicom", studyInstanceUid);

            Assert.Throws<InvalidIdentifierException>(() => StoreRequestValidator.ValidateRequest(request));
        }
    }
}
