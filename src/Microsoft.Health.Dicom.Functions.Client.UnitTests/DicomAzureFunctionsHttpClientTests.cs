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
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests
{
    public class DicomAzureFunctionsHttpClientTests
    {
        private static readonly IOptions<FunctionsClientConfiguration> DefaultConfig = Options.Create(
            new FunctionsClientConfiguration
            {
                BaseAddress = new Uri("https://dicom.core/unit/tests/", UriKind.Absolute),
                Routes = new OperationRoutesConfiguration
                {
                    StatusTemplate = "Operations/{0}",
                }
            });

        [Fact]
        public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(null, DefaultConfig));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(new HttpClient(handler), null));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(
                    new HttpClient(handler),
                    Options.Create<FunctionsClientConfiguration>(null)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t  \r\n")]
        public async Task GivenInvalidId_WhenGettingStatus_ThenThrowArgumentException(string id)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            Type exceptionType = id is null ? typeof(ArgumentNullException) : typeof(ArgumentException);
            await Assert.ThrowsAsync(exceptionType, () => client.GetStatusAsync(id, CancellationToken.None));

            Assert.Equal(0, handler.SentMessages);
        }

        [Fact]
        public async Task GivenNotFound_WhenGettingStatus_ThenReturnNull()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

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
        public async Task GivenUnsuccessfulStatusCode_WhenGettingStatus_ThenThrowHttpRequestException(HttpStatusCode expected)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(expected));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            string id = Guid.NewGuid().ToString();
            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) => AssertExpectedRequest(msg, id);
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStatusAsync(id, source.Token));
            Assert.Equal(expected, ex.StatusCode);
            Assert.Equal(1, handler.SentMessages);
        }

        [Fact]
        public async Task GivenSuccessfulResponse_WhenGettingStatus_ThenReturnStatus()
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
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            using var source = new CancellationTokenSource();

            handler.Sending += (msg, token) => AssertExpectedRequest(msg, id);
            OperationStatusResponse actual = await client.GetStatusAsync(id, source.Token);
            Assert.Equal(1, handler.SentMessages);

            Assert.NotNull(actual);
            Assert.Equal(createdDateTime, actual.CreatedTime);
            Assert.Equal(id, actual.OperationId);
            Assert.Equal(OperationRuntimeStatus.Running, actual.Status);
            Assert.Equal(OperationType.Reindex, actual.Type);
        }

        private static void AssertExpectedRequest(HttpRequestMessage msg, string expectedId)
        {
            Assert.Equal(HttpMethod.Get, msg.Method);
            Assert.Equal("https://dicom.core/unit/tests/Operations/" + expectedId, msg.RequestUri.ToString());
        }
    }
}
