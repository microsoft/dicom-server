// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class HttpResponseExtensionsTests
    {
        [Fact]
        public void AddLocationHeader_GivenNullArguments_ThrowsArgumentNullException()
        {
            var context = new DefaultHttpContext();
            var uri = new Uri("https://example.host.com/unit/tests?method=AddLocationHeader#GivenNullArguments_ThrowException");

            Assert.Throws<ArgumentNullException>(() => HttpResponseExtensions.AddLocationHeader(null, uri));
            Assert.Throws<ArgumentNullException>(() => HttpResponseExtensions.AddLocationHeader(context.Response, null));
        }

        [Theory]
        [InlineData("https://absolute.url:8080/there%20are%20spaces")]
        [InlineData("/relative/url?with=query&string=and#fragment")]
        [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "XUnit more easily leverages compile-time values.")]
        public void AddLocationHeader_GivenValidArguments_AddsHeader(string url)
        {
            var response = new DefaultHttpContext().Response;
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            Assert.False(response.Headers.ContainsKey(HeaderNames.Location));
            response.AddLocationHeader(uri);

            Assert.True(response.Headers.TryGetValue(HeaderNames.Location, out StringValues headerValue));
            Assert.Single(headerValue);
            Assert.Equal(url, headerValue[0]); // Should continue to be escaped!
        }
    }
}
