// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests
{
    public class DicomAzureFunctionsHttpClientTests
    {
        private static readonly IOptions<FunctionsClientOptions> DefaultConfig = Options.Create(
            new FunctionsClientOptions
            {
                BaseAddress = new Uri("https://dicom.core/unit/tests/", UriKind.Absolute),
                Routes = new OperationRoutes
                {
                    StartQueryTagIndexingRoute = new Uri("Reindex", UriKind.Relative),
                    GetStatusRouteTemplate = "Orchestrations/Instances/{0}",
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
                    Options.Create<FunctionsClientOptions>(null)));
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

            handler.SendingAsync += (msg, token) => AssertExpectedStatusRequestAsync(msg, id);
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

            handler.SendingAsync += (msg, token) => AssertExpectedStatusRequestAsync(msg, id);
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
  ""OperationId"": ""{id}"",
  ""Type"": ""Reindex"",
  ""CreatedTime"": ""{createdDateTime}"",
  ""LastUpdatedTime"": ""{createdDateTime.AddMinutes(15)}"",
  ""Status"": ""Running""
}}
"
                )
            };
            var handler = new MockMessageHandler(response);
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            using var source = new CancellationTokenSource();

            handler.SendingAsync += (msg, token) => AssertExpectedStatusRequestAsync(msg, id);
            OperationStatusResponse actual = await client.GetStatusAsync(id, source.Token);
            Assert.Equal(1, handler.SentMessages);

            Assert.NotNull(actual);
            Assert.Equal(createdDateTime, actual.CreatedTime);
            Assert.Equal(createdDateTime.AddMinutes(15), actual.LastUpdatedTime);
            Assert.Equal(id, actual.OperationId);
            Assert.Equal(OperationRuntimeStatus.Running, actual.Status);
            Assert.Equal(OperationType.Reindex, actual.Type);
        }

        [Fact]
        public async Task GivenNullTagKEys_WhenStartingReindex_ThenThrowArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.StartQueryTagIndexingAsync(null, CancellationToken.None));

            Assert.Equal(0, handler.SentMessages);
        }

        [Fact]
        public async Task GivenNoTagKeys_WhenStartingReindex_ThenThrowArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.StartQueryTagIndexingAsync(Array.Empty<int>(), CancellationToken.None));

            Assert.Equal(0, handler.SentMessages);
        }

        [Theory]
        [InlineData(HttpStatusCode.TemporaryRedirect)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GivenUnsuccessfulStatusCode_WhenStartingReindex_ThenThrowHttpRequestException(HttpStatusCode expected)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(expected));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            var input = new List<int> { 1, 2, 3 };
            using var source = new CancellationTokenSource();

            handler.SendingAsync += (msg, token) => AssertExpectedStartAddRequestAsync(msg, input);
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(
                () => client.StartQueryTagIndexingAsync(input, source.Token));

            Assert.Equal(expected, ex.StatusCode);
            Assert.Equal(1, handler.SentMessages);
        }

        [Fact]
        public async Task GivenConflict_WhenStartingReindex_ThenThrowAlreadyExistsException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict));
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            var input = new List<int> { 1, 2, 3 };
            using var source = new CancellationTokenSource();

            handler.SendingAsync += (msg, token) => AssertExpectedStartAddRequestAsync(msg, input);
            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(
                () => client.StartQueryTagIndexingAsync(input, source.Token));
        }

        [Fact]
        public async Task GivenSuccessfulResponse_WhenStartingReindex_ThenReturnInstanceId()
        {
            string expected = Guid.NewGuid().ToString();
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(expected) };
            var handler = new MockMessageHandler(response);
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), DefaultConfig);

            using var source = new CancellationTokenSource();
            var input = new List<int> { 1, 2, 3 };

            handler.SendingAsync += (msg, token) => AssertExpectedStartAddRequestAsync(msg, input);
            string actual = await client.StartQueryTagIndexingAsync(input, source.Token);

            Assert.Equal(1, handler.SentMessages);
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        private static async Task AssertExpectedStatusRequestAsync(HttpRequestMessage msg, string expectedId)
        {
            await AssertExpectedRequestAsync(msg, HttpMethod.Get, new Uri("https://dicom.core/unit/tests/Orchestrations/Instances/" + expectedId));
            Assert.Equal(MediaTypeNames.Application.Json, msg.Headers.Accept.Single().MediaType);
        }

        private static async Task AssertExpectedStartAddRequestAsync(HttpRequestMessage msg, IReadOnlyList<int> tags)
        {
            await AssertExpectedRequestAsync(msg, HttpMethod.Post, new Uri("https://dicom.core/unit/tests/Reindex"));

            var content = msg.Content as StringContent;
            Assert.NotNull(content);
            Assert.Equal(Encoding.UTF8.HeaderName, content.Headers.ContentType.CharSet);
            Assert.Equal(MediaTypeNames.Application.Json, content.Headers.ContentType.MediaType);
            Assert.Equal(
                JsonConvert.SerializeObject(tags, DicomAzureFunctionsHttpClient.JsonSettings),
                await content.ReadAsStringAsync());
        }

        private static Task AssertExpectedRequestAsync(HttpRequestMessage msg, HttpMethod expectedMethod, Uri expectedUri)
        {
            Assert.Equal(expectedMethod, msg.Method);
            Assert.Equal(expectedUri, msg.RequestUri);
            return Task.CompletedTask;
        }
    }
}
