// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Operations
{
    public class DicomDurableFunctionsHttpClientTests
    {
        private static readonly IOptions<OperationsConfiguration> DefaultConfig = Options.Create(
            new OperationsConfiguration
            {
                BaseAddress = new Uri("https://dicom.core/unit/tests/", UriKind.Absolute),
                StatusRouteTemplate = "Operations/{0}",
            });

        [Fact]
        public async Task GetStatusAsync_GivenUnknownId_ReturnNull()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomDurableFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            string id = Guid.NewGuid().ToString();
            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) =>
            {
                Assert.Equal(HttpMethod.Get, msg.Method);
                Assert.Equal("https://dicom.core/unit/tests/Operations/" + id, msg.RequestUri.ToString());
            };
            Assert.Null(await client.GetStatusAsync(id, source.Token));
            Assert.Equal(1, handler.SentMessages);
        }

        [Theory]
        [InlineData(HttpStatusCode.TemporaryRedirect)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetStatusAsync_GivenUnsuccessfulStatusCode_ThrowHttpRequestException(HttpStatusCode expected)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(expected));
            var client = new DicomDurableFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            string id = Guid.NewGuid().ToString();
            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) =>
            {
                Assert.Equal(HttpMethod.Get, msg.Method);
                Assert.Equal("https://dicom.core/unit/tests/Operations/" + id, msg.RequestUri.ToString());
            };
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStatusAsync(id, source.Token));
            Assert.Equal(expected, ex.StatusCode);
            Assert.Equal(1, handler.SentMessages);
        }
    }
}
