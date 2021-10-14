// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Responses
{
    public class MultipartItemTests
    {
        [Theory]
        [InlineData("application/dicom")]
        [InlineData("application/dicom+json")]
        public void GivenAValidContentType_ThenMultipartItemShouldIncludeHeader(string contentType)
        {
            var multipartItem = new MultipartItem(contentType, Stream.Null);

            Assert.Equal(multipartItem.Content.Headers.ContentType.ToString(), contentType);
        }

        [Theory]
        [InlineData("application/dicom;foo=bar")]
        [InlineData("@#$%")]
        public void GivenAnInvalidContentType_ThenMultipartItemShouldThrow(string contentType)
        {
            Assert.Throws<FormatException>(() => new MultipartItem(contentType, Stream.Null));
        }
    }
}
