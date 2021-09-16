// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests
{
    public class DicomAzureFunctionsHttpClientTests
    {
        private readonly IOptions<JsonSerializerOptions> _jsonSerializerOptions;

        private static readonly IOptions<FunctionsClientOptions> DefaultOptions = Options.Create(
            new FunctionsClientOptions
            {
                BaseAddress = new Uri("https://dicom.core/unit/tests/", UriKind.Absolute),
                Routes = new OperationRoutes
                {
                    StartQueryTagIndexingRoute = new Uri("Reindex", UriKind.Relative),
                    GetStatusRouteTemplate = "Orchestrations/Instances/{0}",
                }
            });

        public DicomAzureFunctionsHttpClientTests()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            _jsonSerializerOptions = Options.Create(options);
        }

        [Fact]
        public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var resolver = Substitute.For<IUrlResolver>();
            var store = Substitute.For<IExtendedQueryTagStore>();

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(null, resolver, store, _jsonSerializerOptions, DefaultOptions));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(new HttpClient(handler), null, store, _jsonSerializerOptions, DefaultOptions));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(new HttpClient(handler), resolver, null, _jsonSerializerOptions, DefaultOptions));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(new HttpClient(handler), resolver, store, null, DefaultOptions));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(new HttpClient(handler), resolver, store, _jsonSerializerOptions, null));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsHttpClient(
                    new HttpClient(handler),
                    resolver,
                    store,
                    _jsonSerializerOptions,
                    Options.Create<FunctionsClientOptions>(null)));
        }

        [Fact]
        public async Task GivenNotFound_WhenGettingStatus_ThenReturnNull()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var resolver = Substitute.For<IUrlResolver>();
            var store = Substitute.For<IExtendedQueryTagStore>();
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), resolver, store, _jsonSerializerOptions, DefaultOptions);

            Guid id = Guid.NewGuid();
            using var source = new CancellationTokenSource();

            handler.SendingAsync += (msg, token) => AssertExpectedStatusRequestAsync(msg, id);
            Assert.Null(await client.GetStatusAsync(id, source.Token));
            Assert.Equal(1, handler.SentMessages);
            resolver.DidNotReceiveWithAnyArgs().ResolveQueryTagUri(default);
        }

        [Theory]
        [InlineData(HttpStatusCode.TemporaryRedirect)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GivenUnsuccessfulStatusCode_WhenGettingStatus_ThenThrowHttpRequestException(HttpStatusCode expected)
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(expected));
            var resolver = Substitute.For<IUrlResolver>();
            var store = Substitute.For<IExtendedQueryTagStore>();
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), resolver, store, _jsonSerializerOptions, DefaultOptions);

            Guid id = Guid.NewGuid();
            using var source = new CancellationTokenSource();

            handler.SendingAsync += (msg, token) => AssertExpectedStatusRequestAsync(msg, id);
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStatusAsync(id, source.Token));
            Assert.Equal(expected, ex.StatusCode);
            Assert.Equal(1, handler.SentMessages);
            resolver.DidNotReceiveWithAnyArgs().ResolveQueryTagUri(default);
        }

        [Fact]
        public async Task GivenSuccessfulResponse_WhenGettingStatus_ThenReturnStatus()
        {
            Guid id = Guid.NewGuid();
            var createdDateTime = new DateTime(2021, 06, 08, 1, 2, 3, DateTimeKind.Utc);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
@$"
{{
  ""{nameof(InternalOperationStatus.OperationId)}"": ""{id}"",
  ""{nameof(InternalOperationStatus.Type)}"": ""{OperationType.Reindex}"",
  ""{nameof(InternalOperationStatus.CreatedTime)}"": ""{createdDateTime:O}"",
  ""{nameof(InternalOperationStatus.LastUpdatedTime)}"": ""{createdDateTime.AddMinutes(15):O}"",
  ""{nameof(InternalOperationStatus.Status)}"": ""{OperationRuntimeStatus.Running}"",
  ""{nameof(InternalOperationStatus.PercentComplete)}"": 47,
  ""{nameof(InternalOperationStatus.ResourceIds)}"": [ ""1"", ""4"" ]
}}
"
                ),
            };
            var handler = new MockMessageHandler(response);
            var resolver = Substitute.For<IUrlResolver>();
            var store = Substitute.For<IExtendedQueryTagStore>();
            var client = new DicomAzureFunctionsHttpClient(new HttpClient(handler), resolver, store, _jsonSerializerOptions, DefaultOptions);

            using var source = new CancellationTokenSource();

            List<Uri> expectedResourceUrls = new List<Uri>
            {
                new Uri("https://dicom.core/unit/tests/extendedquerytags/00101010", UriKind.Absolute),
                new Uri("https://dicom.core/unit/tests/extendedquerytags/00104040", UriKind.Absolute),

            };
            resolver.ResolveQueryTagUri("00101010").Returns(expectedResourceUrls[0]);
            resolver.ResolveQueryTagUri("00104040").Returns(expectedResourceUrls[1]);
            store
                .GetExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(new int[] { 1, 4 })),
                    source.Token)
                .Returns(
                    new List<ExtendedQueryTagStoreJoinEntry>
                    {
                        new ExtendedQueryTagStoreJoinEntry(1, "00101010", "AS", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0, id),
                        new ExtendedQueryTagStoreJoinEntry(4, "00104040", "DT", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryStatus.Enabled, 0, id),
                    });
            handler.SendingAsync += (msg, token) => AssertExpectedStatusRequestAsync(msg, id);

            OperationStatus actual = await client.GetStatusAsync(id, source.Token);
            Assert.Equal(1, handler.SentMessages);

            Assert.NotNull(actual);
            Assert.Equal(createdDateTime, actual.CreatedTime);
            Assert.Equal(createdDateTime.AddMinutes(15), actual.LastUpdatedTime);
            Assert.Equal(id, actual.OperationId);
            Assert.Equal(OperationRuntimeStatus.Running, actual.Status);
            Assert.Equal(OperationType.Reindex, actual.Type);
            Assert.Equal(47, actual.PercentComplete);
            Assert.True(actual.Resources.SequenceEqual(expectedResourceUrls));

            await store
                .Received(1)
                .GetExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(new int[] { 1, 4 })),
                    source.Token);
            resolver.Received(1).ResolveQueryTagUri("00101010");
            resolver.Received(1).ResolveQueryTagUri("00104040");
        }

        [Fact]
        public async Task GivenNullTagKeys_WhenStartingReindex_ThenThrowArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomAzureFunctionsHttpClient(
                new HttpClient(handler),
                Substitute.For<IUrlResolver>(),
                Substitute.For<IExtendedQueryTagStore>(),
                _jsonSerializerOptions,
                DefaultOptions);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.StartQueryTagIndexingAsync(null, CancellationToken.None));

            Assert.Equal(0, handler.SentMessages);
        }

        [Fact]
        public async Task GivenNoTagKeys_WhenStartingReindex_ThenThrowArgumentNullException()
        {
            var handler = new MockMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var client = new DicomAzureFunctionsHttpClient(
                new HttpClient(handler),
                Substitute.For<IUrlResolver>(),
                Substitute.For<IExtendedQueryTagStore>(),
                _jsonSerializerOptions,
                DefaultOptions);

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
            var client = new DicomAzureFunctionsHttpClient(
                new HttpClient(handler),
                Substitute.For<IUrlResolver>(),
                Substitute.For<IExtendedQueryTagStore>(),
                _jsonSerializerOptions,
                DefaultOptions);

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
            var client = new DicomAzureFunctionsHttpClient(
                new HttpClient(handler),
                Substitute.For<IUrlResolver>(),
                Substitute.For<IExtendedQueryTagStore>(),
                _jsonSerializerOptions,
                DefaultOptions);

            var input = new List<int> { 1, 2, 3 };
            using var source = new CancellationTokenSource();

            handler.SendingAsync += (msg, token) => AssertExpectedStartAddRequestAsync(msg, input);
            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(
                () => client.StartQueryTagIndexingAsync(input, source.Token));
        }

        [Fact]
        public async Task GivenSuccessfulResponse_WhenStartingReindex_ThenReturnInstanceId()
        {
            Guid expected = Guid.NewGuid();
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(OperationId.ToString(expected)) };
            var handler = new MockMessageHandler(response);
            var client = new DicomAzureFunctionsHttpClient(
                new HttpClient(handler),
                Substitute.For<IUrlResolver>(),
                Substitute.For<IExtendedQueryTagStore>(),
                _jsonSerializerOptions,
                DefaultOptions);

            using var source = new CancellationTokenSource();
            var input = new List<int> { 1, 2, 3 };

            handler.SendingAsync += (msg, token) => AssertExpectedStartAddRequestAsync(msg, input);
            Guid actual = await client.StartQueryTagIndexingAsync(input, source.Token);

            Assert.Equal(1, handler.SentMessages);
            Assert.Equal(expected, actual);
        }

        private static async Task AssertExpectedStatusRequestAsync(HttpRequestMessage msg, Guid expectedId)
        {
            await AssertExpectedRequestAsync(msg, HttpMethod.Get, new Uri("https://dicom.core/unit/tests/Orchestrations/Instances/" + OperationId.ToString(expectedId)));
            Assert.Equal(MediaTypeNames.Application.Json, msg.Headers.Accept.Single().MediaType);
        }

        private async Task AssertExpectedStartAddRequestAsync(HttpRequestMessage msg, IReadOnlyList<int> tags)
        {
            await AssertExpectedRequestAsync(msg, HttpMethod.Post, new Uri("https://dicom.core/unit/tests/Reindex"));

            var content = msg.Content as JsonContent;
            Assert.NotNull(content);
            Assert.Equal(JsonSerializer.Serialize(tags, _jsonSerializerOptions.Value), await content.ReadAsStringAsync());
        }

        private static Task AssertExpectedRequestAsync(HttpRequestMessage msg, HttpMethod expectedMethod, Uri expectedUri)
        {
            Assert.Equal(expectedMethod, msg.Method);
            Assert.Equal(expectedUri, msg.RequestUri);
            return Task.CompletedTask;
        }
    }
}
