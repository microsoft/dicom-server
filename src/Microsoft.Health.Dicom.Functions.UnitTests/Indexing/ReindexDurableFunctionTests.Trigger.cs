// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenNoInput_WhenStartingToReindexInstances_ThenReturnBadRequest()
        {
            HttpResponseMessage actual;
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            // Null
            actual = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(null),
                client,
                NullLogger.Instance);

            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);

            // Empty
            actual = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(new List<int>()),
                client,
                NullLogger.Instance);

            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
        }

        [Fact]
        public async Task GivenExtendedQueryTagConflict_WhenStartingToReindexInstances_ThenReturnConflict()
        {
            Guid instanceId = Guid.NewGuid();
            var expectedTagKeys = new List<int> { 1, 2, 3 };
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            _guidFactory.Create().Returns(instanceId);
            client
                .StartNewAsync(
                    nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    OperationId.ToString(instanceId),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(expectedTagKeys)))
                .Returns(OperationId.ToString(instanceId));
            _extendedQueryTagStore
                .AssignReindexingOperationAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTagKeys)),
                    instanceId,
                    returnIfCompleted: true,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new List<ExtendedQueryTagStoreEntry>());

            HttpResponseMessage response = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(expectedTagKeys),
                client,
                NullLogger.Instance);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            _guidFactory.Received(1).Create();
            await client
                .Received(1)
                .StartNewAsync(
                    nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    OperationId.ToString(instanceId),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(expectedTagKeys)));
            await _extendedQueryTagStore
                .Received(1)
                .AssignReindexingOperationAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTagKeys)),
                    instanceId,
                    returnIfCompleted: true,
                    cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenExtendedQueryTagKeys_WhenStartingToReindexInstances_ThenReturnOperationId()
        {
            Guid instanceId = Guid.NewGuid();
            var expectedTagKeys = new List<int> { 1, 2, 3 };
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            _guidFactory.Create().Returns(instanceId);
            client
                .StartNewAsync(
                    nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    OperationId.ToString(instanceId),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(expectedTagKeys)))
                .Returns(OperationId.ToString(instanceId));
            _extendedQueryTagStore
                .AssignReindexingOperationAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTagKeys)),
                    instanceId,
                    returnIfCompleted: true,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(
                    new List<ExtendedQueryTagStoreEntry>
                    {
                        new ExtendedQueryTagStoreEntry(1, "1", "DA", "foo", QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, null)
                    });

            HttpResponseMessage response = await _reindexDurableFunction.StartReindexingInstancesAsync(
                CreateRequest(expectedTagKeys),
                client,
                NullLogger.Instance);

            var content = response.Content as StringContent;
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal(OperationId.ToString(instanceId), await response.Content.ReadAsStringAsync());

            _guidFactory.Received(1).Create();
            await client
                .Received(1)
                .StartNewAsync(
                    nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    OperationId.ToString(instanceId),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(expectedTagKeys)));
            await _extendedQueryTagStore
                .Received(1)
                .AssignReindexingOperationAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTagKeys)),
                    instanceId,
                    returnIfCompleted: true,
                    cancellationToken: Arg.Any<CancellationToken>());
        }

        private HttpRequest CreateRequest(IReadOnlyCollection<int> tagKeys)
        {
            byte[] buffer = tagKeys == null ? Array.Empty<byte>() : JsonSerializer.SerializeToUtf8Bytes(tagKeys, _jsonSerializerOptions);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethod.Post.Method;
            context.Request.Host = new HostString("https://functions.dicom");
            context.Request.Path = new PathString("/unit/test/reindexing");
            context.Request.Body = buffer == null ? Stream.Null : new MemoryStream(buffer);
            context.Request.ContentType = MediaTypeNames.Application.Json;
            context.Request.ContentLength = buffer.Length;

            return context.Request;
        }
    }
}
