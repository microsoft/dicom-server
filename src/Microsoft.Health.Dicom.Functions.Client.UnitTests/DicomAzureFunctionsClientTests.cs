// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.DurableTask;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests
{
    public class DicomAzureFunctionsClientTests
    {
        private readonly IDurableClient _durableClient;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IUrlResolver _urlResolver;
        private readonly IGuidFactory _guidFactory;
        private readonly DicomAzureFunctionsClient _client;

        public DicomAzureFunctionsClientTests()
        {
            IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
            _durableClient = Substitute.For<IDurableClient>();
            durableClientFactory.CreateClient().Returns(_durableClient);

            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _urlResolver = Substitute.For<IUrlResolver>();
            _guidFactory = Substitute.For<IGuidFactory>();
            _client = new DicomAzureFunctionsClient(
                durableClientFactory,
                _extendedQueryTagStore,
                _urlResolver,
                _guidFactory,
                NullLogger<DicomAzureFunctionsClient>.Instance);
        }

        [Fact]
        public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
        {
            IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
            IExtendedQueryTagStore extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            IGuidFactory guidFactory = Substitute.For<IGuidFactory>();

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsClient(null, extendedQueryTagStore, urlResolver, guidFactory, NullLogger<DicomAzureFunctionsClient>.Instance));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsClient(durableClientFactory, null, urlResolver, guidFactory, NullLogger<DicomAzureFunctionsClient>.Instance));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, null, guidFactory, NullLogger<DicomAzureFunctionsClient>.Instance));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, urlResolver, null, NullLogger<DicomAzureFunctionsClient>.Instance));

            Assert.Throws<ArgumentNullException>(
                () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, urlResolver, guidFactory, null));
        }

        [Fact]
        public async Task GivenNotFound_WhenGettingStatus_ThenReturnNull()
        {
            Guid id = Guid.NewGuid();
            using var source = new CancellationTokenSource();

            _durableClient.GetStatusAsync(OperationId.ToString(id), showInput: true).Returns(Task.FromResult<DurableOrchestrationStatus>(null));

            Assert.Null(await _client.GetStatusAsync(id, source.Token));

            await _durableClient.Received(1).GetStatusAsync(OperationId.ToString(id), showInput: true);
            await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().GetExtendedQueryTagsAsync((IReadOnlyList<int>)default);
            _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagUri(default);
        }

        [Fact]
        public async Task GivenUnknownName_WhenGettingStatus_ThenReturnNull()
        {
            Guid id = Guid.NewGuid();
            using var source = new CancellationTokenSource();

            _durableClient
                .GetStatusAsync(OperationId.ToString(id), showInput: true)
                .Returns(new DurableOrchestrationStatus
                {
                    InstanceId = OperationId.ToString(id),
                    Name = "Foobar",
                    RuntimeStatus = OrchestrationRuntimeStatus.Running,
                });

            Assert.Null(await _client.GetStatusAsync(id, source.Token));

            await _durableClient.Received(1).GetStatusAsync(OperationId.ToString(id), showInput: true);
            await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().GetExtendedQueryTagsAsync((IReadOnlyList<int>)default);
            _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagUri(default);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GivenReindexOperation_WhenGettingStatus_ThenReturnStatus(bool populateInput)
        {
            Guid id = Guid.NewGuid();
            var createdDateTime = new DateTime(2021, 06, 08, 1, 2, 3, DateTimeKind.Utc);
            var tagKeys = new int[] { 1, 2, 3 };
            var tagPaths = tagKeys.Select(x => string.Join("", Enumerable.Repeat(x.ToString("D2", CultureInfo.InvariantCulture), 4))).ToArray();
            var expectedResourceUrls = tagPaths.Select(x => new Uri($"https://dicom-unit-tests/extendedquerytags/{x}", UriKind.Absolute)).ToArray();

            using var source = new CancellationTokenSource();

            _durableClient
                .GetStatusAsync(OperationId.ToString(id), showInput: true)
                .Returns(new DurableOrchestrationStatus
                {
                    CreatedTime = createdDateTime,
                    CustomStatus = null,
                    History = null,
                    Input = populateInput
                        ? JObject.FromObject(
                            new ReindexInput
                            {
                                Completed = new WatermarkRange(21, 100),
                                QueryTagKeys = tagKeys,
                            })
                        : null,
                    InstanceId = OperationId.ToString(id),
                    LastUpdatedTime = createdDateTime.AddMinutes(15),
                    Name = FunctionNames.ReindexInstances,
                    Output = null,
                    RuntimeStatus = OrchestrationRuntimeStatus.Running,
                });
            _extendedQueryTagStore
                .GetExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyCollection<int>>(x => x.SequenceEqual(tagKeys)),
                    source.Token)
                .Returns(
                    tagPaths
                        .Select((path, i) => new ExtendedQueryTagStoreJoinEntry(
                            tagKeys[i],
                            path,
                            "AS",
                            null,
                            QueryTagLevel.Instance,
                            ExtendedQueryTagStatus.Adding,
                            QueryStatus.Enabled,
                            0,
                            id))
                        .ToList());

            for (int i = 0; i < tagPaths.Length; i++)
            {
                _urlResolver.ResolveQueryTagUri(tagPaths[i]).Returns(expectedResourceUrls[i]);
            }

            OperationStatus actual = await _client.GetStatusAsync(id, source.Token);
            Assert.NotNull(actual);
            Assert.Equal(createdDateTime, actual.CreatedTime);
            Assert.Equal(createdDateTime.AddMinutes(15), actual.LastUpdatedTime);
            Assert.Equal(id, actual.OperationId);
            Assert.Equal(populateInput ? 80 : 0, actual.PercentComplete);
            Assert.True(actual.Resources.SequenceEqual(populateInput ? expectedResourceUrls : Array.Empty<Uri>()));
            Assert.Equal(OperationRuntimeStatus.Running, actual.Status);
            Assert.Equal(OperationType.Reindex, actual.Type);

            await _durableClient.Received(1).GetStatusAsync(OperationId.ToString(id), showInput: true);

            if (populateInput)
            {
                await _extendedQueryTagStore
                .Received(1)
                .GetExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyCollection<int>>(x => x.SequenceEqual(tagKeys)),
                    source.Token);
                foreach (string path in tagPaths)
                {
                    _urlResolver.Received(1).ResolveQueryTagUri(path);
                }
            }
        }

        [Fact]
        public async Task GivenNullTagKeys_WhenStartingReindex_ThenThrowArgumentNullException()
        {
            using var source = new CancellationTokenSource();

            await Assert.ThrowsAsync<ArgumentNullException>(() => _client.StartReindexingInstancesAsync(null, source.Token));

            _guidFactory.DidNotReceiveWithAnyArgs().Create();
            await _durableClient.DidNotReceiveWithAnyArgs().StartNewAsync(default, default);
            await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().AssignReindexingOperationAsync(default, default);
        }

        [Fact]
        public async Task GivenNoTagKeys_WhenStartingReindex_ThenThrowArgumentException()
        {
            using var source = new CancellationTokenSource();

            await Assert.ThrowsAsync<ArgumentException>(() => _client.StartReindexingInstancesAsync(Array.Empty<int>(), source.Token));

            _guidFactory.DidNotReceiveWithAnyArgs().Create();
            await _durableClient.DidNotReceiveWithAnyArgs().StartNewAsync(default, default);
            await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().AssignReindexingOperationAsync(default, default);
        }

        [Fact]
        public async Task GivenNoAssignedKeys_WhenStartingReindex_ThenThrowAlreadyExistsException()
        {
            Guid id = Guid.NewGuid();
            int[] tagKeys = new int[] { 10, 42 };
            using var source = new CancellationTokenSource();

            _guidFactory.Create().Returns(id);
            _durableClient
                .StartNewAsync(
                    FunctionNames.ReindexInstances,
                    OperationId.ToString(id),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)))
                .Returns(OperationId.ToString(id));
            _extendedQueryTagStore
                .AssignReindexingOperationAsync(tagKeys, id, true, source.Token)
                .Returns(Array.Empty<ExtendedQueryTagStoreEntry>());

            await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => _client.StartReindexingInstancesAsync(tagKeys, source.Token));

            _guidFactory.Received(1).Create();
            await _durableClient
                .Received(1)
                .StartNewAsync(
                    FunctionNames.ReindexInstances,
                    OperationId.ToString(id),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)));
            await _extendedQueryTagStore.Received(1).AssignReindexingOperationAsync(tagKeys, id, true, source.Token);
        }

        [Fact]
        public async Task GivenAssignedKeys_WhenStartingReindex_ThenReturnInstanceId()
        {
            Guid id = Guid.NewGuid();
            int[] tagKeys = new int[] { 10, 42 };
            using var source = new CancellationTokenSource();

            _guidFactory.Create().Returns(id);
            _durableClient
                .StartNewAsync(
                    FunctionNames.ReindexInstances,
                    OperationId.ToString(id),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)))
                .Returns(OperationId.ToString(id));
            _extendedQueryTagStore
                .AssignReindexingOperationAsync(tagKeys, id, true, source.Token)
                .Returns(tagKeys
                    .Select(x => new ExtendedQueryTagStoreEntry(
                        x,
                        x.ToString(),
                        "DA",
                        null,
                        QueryTagLevel.Study,
                        ExtendedQueryTagStatus.Adding,
                        QueryStatus.Enabled,
                        0))
                    .ToArray());

            Assert.Equal(id, await _client.StartReindexingInstancesAsync(tagKeys, source.Token));

            _guidFactory.Received(1).Create();
            await _durableClient
                .Received(1)
                .StartNewAsync(
                    FunctionNames.ReindexInstances,
                    OperationId.ToString(id),
                    Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)));
            await _extendedQueryTagStore.Received(1).AssignReindexingOperationAsync(tagKeys, id, true, source.Token);
        }
    }
}
