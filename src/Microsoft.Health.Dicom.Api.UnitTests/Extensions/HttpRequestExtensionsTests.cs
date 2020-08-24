// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class HttpRequestExtensionsTests
    {
        [Fact]
        public void GivenEmptyHeaders_WhenGetAcceptHeaders_ThenShouldReturnEmpty()
        {
            var httpRequest = Substitute.For<HttpRequest>();
            IHeaderDictionary headers = new HeaderDictionary();
            httpRequest.Headers.Returns(headers);
            IEnumerable<AcceptHeader> acceptHeaders = httpRequest.GetAcceptHeaders();
            Assert.Empty(acceptHeaders);
        }

        [Fact]
        public void GivenNonEmptyHeaders_WhenGetAcceptHeaders_ThenShouldReturnHeaders()
        {
            var httpRequest = Substitute.For<HttpRequest>();
            IHeaderDictionary headers = new HeaderDictionary();
            headers.Add("accept", "application/dicom");
            httpRequest.Headers.Returns(headers);
            IEnumerable<AcceptHeader> acceptHeaders = httpRequest.GetAcceptHeaders();
            Assert.Single(acceptHeaders);
        }

        [Fact]
        public void GivenNullHttpRequest_WhenGetAcceptHeaders_ThenShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => HttpRequestExtensions.GetAcceptHeaders(null));
        }
    }
}
