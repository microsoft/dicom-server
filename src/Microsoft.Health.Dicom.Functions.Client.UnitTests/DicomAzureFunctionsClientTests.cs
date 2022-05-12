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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.DurableTask;
using Microsoft.Health.Operations;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests;

public class DicomAzureFunctionsClientTests
{
    private readonly IDurableClient _durableClient;
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IUrlResolver _urlResolver;
    private readonly IGuidFactory _guidFactory;
    private readonly DicomFunctionOptions _options;
    private readonly DicomAzureFunctionsClient _client;

    public DicomAzureFunctionsClientTests()
    {
        IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
        _durableClient = Substitute.For<IDurableClient>();
        durableClientFactory.CreateClient().Returns(_durableClient);

        _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        _urlResolver = Substitute.For<IUrlResolver>();
        _guidFactory = Substitute.For<IGuidFactory>();
        _options = new DicomFunctionOptions
        {
            Export = new FanOutFunctionOptions
            {
                Name = FunctionNames.ExportDicomFiles,
                Batching = new BatchingOptions
                {
                    MaxParallelCount = 2,
                    Size = 50,
                },
            },
            Indexing = new FanOutFunctionOptions
            {
                Name = FunctionNames.ReindexInstances,
                Batching = new BatchingOptions
                {
                    MaxParallelCount = 1,
                    Size = 100,
                },
            }
        };
        _client = new DicomAzureFunctionsClient(
            durableClientFactory,
            _extendedQueryTagStore,
            _urlResolver,
            _guidFactory,
            Options.Create(_options),
            NullLogger<DicomAzureFunctionsClient>.Instance);
    }

    [Fact]
    public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
    {
        IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
        IExtendedQueryTagStore extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
        IGuidFactory guidFactory = Substitute.For<IGuidFactory>();
        var options = Options.Create(new DicomFunctionOptions());

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(null, extendedQueryTagStore, urlResolver, guidFactory, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, null, urlResolver, guidFactory, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, null, guidFactory, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, urlResolver, null, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, urlResolver, guidFactory, null, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, extendedQueryTagStore, urlResolver, guidFactory, options, null));
    }

    [Fact]
    public async Task GivenNotFound_WhenGettingState_ThenReturnNull()
    {
        string instanceId = OperationId.Generate();
        using var source = new CancellationTokenSource();

        _durableClient.GetStatusAsync(instanceId, showInput: true).Returns(Task.FromResult<DurableOrchestrationStatus>(null));

        Assert.Null(await _client.GetStateAsync(Guid.Parse(instanceId), source.Token));

        await _durableClient.Received(1).GetStatusAsync(instanceId, showInput: true);
        await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().GetExtendedQueryTagsAsync((IReadOnlyList<int>)default);
        _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagUri(default);
    }

    [Fact]
    public async Task GivenUnknownName_WhenGettingState_ThenReturnNull()
    {
        string instanceId = OperationId.Generate();
        using var source = new CancellationTokenSource();

        _durableClient
            .GetStatusAsync(instanceId, showInput: true)
            .Returns(new DurableOrchestrationStatus
            {
                InstanceId = instanceId,
                Name = "Foobar",
                RuntimeStatus = OrchestrationRuntimeStatus.Running,
            });

        Assert.Null(await _client.GetStateAsync(Guid.Parse(instanceId), source.Token));

        await _durableClient.Received(1).GetStatusAsync(instanceId, showInput: true);
        await _extendedQueryTagStore.DidNotReceiveWithAnyArgs().GetExtendedQueryTagsAsync((IReadOnlyList<int>)default);
        _urlResolver.DidNotReceiveWithAnyArgs().ResolveQueryTagUri(default);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task GivenReindexOperation_WhenGettingState_ThenReturnStatus(bool populateInput, bool overrideCreatedTime)
    {
        string instanceId = OperationId.Generate();
        Guid operationId = Guid.Parse(instanceId);
        var createdTime = new DateTime(2021, 06, 08, 1, 2, 3, DateTimeKind.Utc);
        var tagKeys = new int[] { 1, 2, 3 };
        var tagPaths = tagKeys.Select(x => string.Join("", Enumerable.Repeat(x.ToString("D2", CultureInfo.InvariantCulture), 4))).ToArray();
        var expectedResourceUrls = tagPaths.Select(x => new Uri($"https://dicom-unit-tests/extendedquerytags/{x}", UriKind.Absolute)).ToArray();

        using var source = new CancellationTokenSource();

        _durableClient
            .GetStatusAsync(instanceId, showInput: true)
            .Returns(new DurableOrchestrationStatus
            {
                CreatedTime = createdTime,
                CustomStatus = null,
                History = null,
                Input = populateInput
                    ? JObject.FromObject(
                        new ReindexCheckpoint
                        {
                            Completed = new WatermarkRange(21, 100),
                            CreatedTime = overrideCreatedTime ? createdTime.AddHours(-1) : null,
                            QueryTagKeys = tagKeys,
                        })
                    : null,
                InstanceId = instanceId,
                LastUpdatedTime = createdTime.AddMinutes(15),
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
                        operationId))
                    .ToList());

        for (int i = 0; i < tagPaths.Length; i++)
        {
            _urlResolver.ResolveQueryTagUri(tagPaths[i]).Returns(expectedResourceUrls[i]);
        }

        OperationState<DicomOperation> actual = await _client.GetStateAsync(operationId, source.Token);
        Assert.NotNull(actual);
        Assert.Equal(overrideCreatedTime ? createdTime.AddHours(-1) : createdTime, actual.CreatedTime);
        Assert.Equal(createdTime.AddMinutes(15), actual.LastUpdatedTime);
        Assert.Equal(operationId, actual.OperationId);
        Assert.Equal(populateInput ? 80 : 0, actual.PercentComplete);
        Assert.True(actual.Resources.SequenceEqual(populateInput ? expectedResourceUrls : Array.Empty<Uri>()));
        Assert.Equal(OperationStatus.Running, actual.Status);
        Assert.Equal(DicomOperation.Reindex, actual.Type);

        await _durableClient.Received(1).GetStatusAsync(instanceId, showInput: true);

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
        string instanceId = OperationId.Generate();
        Guid operationId = Guid.Parse(instanceId);
        int[] tagKeys = new int[] { 10, 42 };
        using var source = new CancellationTokenSource();

        _guidFactory.Create().Returns(operationId);
        _durableClient
            .StartNewAsync(
                FunctionNames.ReindexInstances,
                instanceId,
                Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)))
            .Returns(instanceId);
        _extendedQueryTagStore
            .AssignReindexingOperationAsync(tagKeys, operationId, true, source.Token)
            .Returns(Array.Empty<ExtendedQueryTagStoreEntry>());

        await Assert.ThrowsAsync<ExtendedQueryTagsAlreadyExistsException>(() => _client.StartReindexingInstancesAsync(tagKeys, source.Token));

        _guidFactory.Received(1).Create();
        await _durableClient
            .Received(1)
            .StartNewAsync(
                FunctionNames.ReindexInstances,
                instanceId,
                Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)));
        await _extendedQueryTagStore.Received(1).AssignReindexingOperationAsync(tagKeys, operationId, true, source.Token);
    }

    [Fact]
    public async Task GivenAssignedKeys_WhenStartingReindex_ThenReturnInstanceId()
    {
        string instanceId = OperationId.Generate();
        Guid operationId = Guid.Parse(instanceId);
        int[] tagKeys = new int[] { 10, 42 };
        using var source = new CancellationTokenSource();

        _guidFactory.Create().Returns(operationId);
        _durableClient
            .StartNewAsync(
                FunctionNames.ReindexInstances,
                instanceId,
                Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)))
            .Returns(instanceId);
        _extendedQueryTagStore
            .AssignReindexingOperationAsync(tagKeys, operationId, true, source.Token)
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

        Assert.Equal(operationId, await _client.StartReindexingInstancesAsync(tagKeys, source.Token));

        _guidFactory.Received(1).Create();
        await _durableClient
            .Received(1)
            .StartNewAsync(
                FunctionNames.ReindexInstances,
                instanceId,
                Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)));
        await _extendedQueryTagStore.Received(1).AssignReindexingOperationAsync(tagKeys, operationId, true, source.Token);
    }

    [Fact]
    public async Task GivenValidArgs_WhenStartingExport_ThenStartOrchestration()
    {
        var operationId = Guid.NewGuid();
        var spec = new ExportSpecification
        {
            Destination = new TypedConfiguration<ExportDestinationType>(),
            Source = new TypedConfiguration<ExportSourceType>(),
        };
        var partition = new PartitionEntry(17, "test");

        _guidFactory.Create().Returns(operationId);
        _durableClient
            .StartNewAsync(
                FunctionNames.ExportDicomFiles,
                operationId.ToString(OperationId.FormatSpecifier),
                Arg.Is<ExportInput>(x => ReferenceEquals(_options.Export.Batching, x.Batching)
                    && ReferenceEquals(spec.Destination, x.Destination)
                    && ReferenceEquals(partition, x.Partition)
                    && ReferenceEquals(spec.Source, x.Source)))
            .Returns(operationId.ToString(OperationId.FormatSpecifier));

        using var tokenSource = new CancellationTokenSource();
        Assert.Equal(operationId, await _client.StartExportingFilesAsync(spec, partition, tokenSource.Token));

        _guidFactory.Received(1).Create();
        await _durableClient
            .Received(1)
            .StartNewAsync(
                FunctionNames.ExportDicomFiles,
                operationId.ToString(OperationId.FormatSpecifier),
                Arg.Is<ExportInput>(x => ReferenceEquals(_options.Export.Batching, x.Batching)
                    && ReferenceEquals(spec.Destination, x.Destination)
                    && ReferenceEquals(partition, x.Partition)
                    && ReferenceEquals(spec.Source, x.Source)));
    }
}
