// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
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

        [Theory]
        [InlineData("application/dicom", "2.25.112888080741846573563165816190995220580")]
        [InlineData("application/dicom+json", "1.3.6.1.4.1.5962.99.1.2280943358.716200484.1363785608958.477.0")]
        public void GivenAValidContentType_ThenMultipartItemShouldContainTransferSyntaxInHeader(string contentType, string transferSyntax)
        {
            var multipartItem = new MultipartItem(contentType, Stream.Null, transferSyntax);
            var parameters = (multipartItem.Content.Headers.ContentType.Parameters).ToList();

            Assert.NotNull(parameters.Find(p => p.Value == transferSyntax));
        }
    }
}
