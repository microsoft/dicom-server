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
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
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
        public void Ctor_GivenNullArguments_ThrowsArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            Assert.Throws<ArgumentNullException>(
                () => new DicomDurableFunctionsHttpClient(null, DefaultConfig));

            Assert.Throws<ArgumentNullException>(
                () => new DicomDurableFunctionsHttpClient(new HttpClient(handler), null));

            Assert.Throws<ArgumentNullException>(
                () => new DicomDurableFunctionsHttpClient(
                    new HttpClient(handler),
                    Options.Create<OperationsConfiguration>(null)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t  \r\n")]
        public async Task GetStatusAsync_GivenInvalidId_ThrowsArgumentException(string id)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomDurableFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            Type exceptionType = id is null ? typeof(ArgumentNullException) : typeof(ArgumentException);
            await Assert.ThrowsAsync(exceptionType, () => client.GetStatusAsync(id, CancellationToken.None));

            Assert.Equal(0, handler.SentMessages);
        }

        [Fact]
        public async Task GetStatusAsync_GivenNotFound_ReturnsNull()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomDurableFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            string id = Guid.NewGuid().ToString();
            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) => AssertExpectedRequest(msg, id);
            Assert.Null(await client.GetStatusAsync(id, source.Token));
            Assert.Equal(1, handler.SentMessages);
        }

        [Theory]
        [InlineData(HttpStatusCode.TemporaryRedirect)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetStatusAsync_GivenUnsuccessfulStatusCode_ThrowsHttpRequestException(HttpStatusCode expected)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(expected));
            var client = new DicomDurableFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            string id = Guid.NewGuid().ToString();
            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) => AssertExpectedRequest(msg, id);
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStatusAsync(id, source.Token));
            Assert.Equal(expected, ex.StatusCode);
            Assert.Equal(1, handler.SentMessages);
        }

        [Fact]
        public async Task GetStatusAsync_GivenSuccessfulResponse_ReturnsStatus()
        {
            string id = Guid.NewGuid().ToString();
            var createdDateTime = new DateTime(2021, 06, 08, 1, 2, 3, DateTimeKind.Utc);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
@$"
{{
  ""Name"": ""Reindex"",
  ""InstanceId"": ""{id}"",
  ""CreatedTime"": ""{createdDateTime}"",
  ""LastUpdatedTime"": ""{createdDateTime.AddMinutes(15)}"",
  ""Input"": null,
  ""Output"": ""Hello World"",
  ""RuntimeStatus"": ""Running"",
  ""CustomStatus"": {{
    ""Foo"": ""Bar""
    }},
  ""History"": null
}}
"
                )
            };
            var handler = new MockMessageHandler(response);
            var client = new DicomDurableFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) => AssertExpectedRequest(msg, id);
            OperationStatusResponse actual = await client.GetStatusAsync(id, source.Token);
            Assert.Equal(1, handler.SentMessages);

            Assert.NotNull(actual);
            Assert.Equal(createdDateTime, actual.CreatedTime);
            Assert.Equal(id, actual.Id);
            Assert.Equal(OperationStatus.Running, actual.Status);
            Assert.Equal(OperationType.Reindex, actual.Type);
        }

        private static void AssertExpectedRequest(HttpRequestMessage msg, string expectedId)
        {
            Assert.Equal(HttpMethod.Get, msg.Method);
            Assert.Equal("https://dicom.core/unit/tests/Operations/" + expectedId, msg.RequestUri.ToString());
        }
    }
}
