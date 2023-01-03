// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Export;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Operations;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests;

public class DicomAzureFunctionsClientTests
{
    private readonly IDurableClient _durableClient;
    private readonly IUrlResolver _urlResolver;
    private readonly IDicomOperationsResourceStore _resourceStore;
    private readonly DicomFunctionOptions _options;
    private readonly DicomAzureFunctionsClient _client;

    public DicomAzureFunctionsClientTests()
    {
        IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
        _durableClient = Substitute.For<IDurableClient>();
        durableClientFactory.CreateClient().Returns(_durableClient);

        _urlResolver = Substitute.For<IUrlResolver>();
        _resourceStore = Substitute.For<IDicomOperationsResourceStore>();
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
            },
        };
        _client = new DicomAzureFunctionsClient(
            durableClientFactory,
            _urlResolver,
            _resourceStore,
            Options.Create(_options),
            NullLogger<DicomAzureFunctionsClient>.Instance);
    }

    [Fact]
    public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
    {
        IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
        IExtendedQueryTagStore extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
        IDicomOperationsResourceStore resourceStore = Substitute.For<IDicomOperationsResourceStore>();
        var options = Options.Create(new DicomFunctionOptions());

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(null, urlResolver, resourceStore, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, null, resourceStore, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, urlResolver, null, options, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, urlResolver, resourceStore, null, NullLogger<DicomAzureFunctionsClient>.Instance));

        Assert.Throws<ArgumentNullException>(
            () => new DicomAzureFunctionsClient(durableClientFactory, urlResolver, resourceStore, options, null));
    }

    [Fact]
    public async Task GivenNotFound_WhenGettingState_ThenReturnNull()
    {
        string instanceId = OperationId.Generate();
        using var source = new CancellationTokenSource();

        _durableClient.GetStatusAsync(instanceId, showInput: true).Returns(Task.FromResult<DurableOrchestrationStatus>(null));

        Assert.Null(await _client.GetStateAsync(Guid.Parse(instanceId), source.Token));

        await _durableClient.Received(1).GetStatusAsync(instanceId, showInput: true);
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

        if (populateInput)
        {
            _resourceStore
                .ResolveQueryTagKeysAsync(
                    Arg.Is<IReadOnlyCollection<int>>(x => x.SequenceEqual(tagKeys)),
                    source.Token)
                .Returns(tagPaths.ToAsyncEnumerable());
        }

        for (int i = 0; i < tagPaths.Length; i++)
        {
            _urlResolver.ResolveQueryTagUri(tagPaths[i]).Returns(expectedResourceUrls[i]);
        }

        IOperationState<DicomOperation> actual = await _client.GetStateAsync(operationId, source.Token);
        Assert.NotNull(actual);
        Assert.Equal(overrideCreatedTime ? createdTime.AddHours(-1) : createdTime, actual.CreatedTime);
        Assert.Equal(createdTime.AddMinutes(15), actual.LastUpdatedTime);
        Assert.Equal(operationId, actual.OperationId);
        Assert.Equal(populateInput ? 80 : 0, actual.PercentComplete);
        Assert.True(actual.Resources.SequenceEqual(populateInput ? expectedResourceUrls : Array.Empty<Uri>()));
        Assert.Null(actual.Results);
        Assert.Equal(OperationStatus.Running, actual.Status);
        Assert.Equal(DicomOperation.Reindex, actual.Type);

        await _durableClient.Received(1).GetStatusAsync(instanceId, showInput: true);

        if (populateInput)
        {
            _resourceStore
                .Received(1)
                .ResolveQueryTagKeysAsync(
                    Arg.Is<IReadOnlyCollection<int>>(x => x.SequenceEqual(tagKeys)),
                    source.Token);

            foreach (string path in tagPaths)
            {
                _urlResolver.Received(1).ResolveQueryTagUri(path);
            }
        }
    }

    [Fact]
    public async Task GivenExportOperation_WhenGettingState_ThenSuccessfullyParseCheckpointWithResults()
    {
        string instanceId = OperationId.Generate();
        Guid operationId = Guid.Parse(instanceId);
        var createdTime = new DateTime(2022, 08, 26, 1, 2, 3, DateTimeKind.Utc);

        using var source = new CancellationTokenSource();

        _durableClient
            .GetStatusAsync(instanceId, showInput: true)
            .Returns(new DurableOrchestrationStatus
            {
                CreatedTime = createdTime,
                CustomStatus = null,
                History = null,
                Input = JObject.FromObject(
                    new ExportCheckpoint
                    {
                        ErrorHref = new Uri($"https://unit-test.blob.core.windows.net/export/{instanceId}/errors.log"),
                        Progress = new ExportProgress(1000, 2),
                    }),
                InstanceId = instanceId,
                LastUpdatedTime = createdTime.AddMinutes(5),
                Name = FunctionNames.ExportDicomFiles,
                Output = null,
                RuntimeStatus = OrchestrationRuntimeStatus.Running,
            });

        IOperationState<DicomOperation> actual = await _client.GetStateAsync(operationId, source.Token);
        Assert.NotNull(actual);
        Assert.Equal(createdTime, actual.CreatedTime);
        Assert.Equal(createdTime.AddMinutes(5), actual.LastUpdatedTime);
        Assert.Equal(operationId, actual.OperationId);
        Assert.Null(actual.PercentComplete);
        Assert.Null(actual.Resources);
        Assert.Equal(OperationStatus.Running, actual.Status);
        Assert.Equal(DicomOperation.Export, actual.Type);

        var results = actual.Results as ExportResults;
        Assert.NotNull(results);
        Assert.Equal(new Uri($"https://unit-test.blob.core.windows.net/export/{instanceId}/errors.log"), results.ErrorHref);
        Assert.Equal(1000L, results.Exported);
        Assert.Equal(2L, results.Skipped);

        await _durableClient.Received(1).GetStatusAsync(instanceId, showInput: true);
    }

    [Fact]
    public async Task GivenCriteria_WhenSearchingForOperations_ThenReturnPaginatedResults()
    {
        using var source = new CancellationTokenSource();

        DateTime end = DateTime.UtcNow;
        DateTime start = end.AddHours(-2);

        var expectedInstances = new List<DurableOrchestrationStatus>
        {
            new DurableOrchestrationStatus { InstanceId = Guid.NewGuid().ToString("N"), Name = "ReindexInstancesAsync" },
            new DurableOrchestrationStatus { InstanceId = "foo", Name = "ReindexInstancesAsync" },
            new DurableOrchestrationStatus { InstanceId = Guid.NewGuid().ToString("N"), Name = "bar" },
            new DurableOrchestrationStatus { InstanceId = Guid.NewGuid().ToString("N"), Name = "ExportDicomFilesAsync" },
            new DurableOrchestrationStatus { InstanceId = Guid.NewGuid().ToString("N"), Name = "ReindexInstancesAsync" },
            new DurableOrchestrationStatus { InstanceId = Guid.NewGuid().ToString("N"), Name = "ReindexInstancesAsync" },
            new DurableOrchestrationStatus { InstanceId = Guid.NewGuid().ToString("N"), Name = "ExportDicomFilesAsync" },
        };

        var expected = new List<OperationReference>
        {
            new OperationReference(Guid.Parse(expectedInstances[0].InstanceId), new Uri("https://dicom.unit.test/operations/" + expectedInstances[0].InstanceId)),
            new OperationReference(Guid.Parse(expectedInstances[4].InstanceId), new Uri("https://dicom.unit.test/operations/" + expectedInstances[4].InstanceId)),
            new OperationReference(Guid.Parse(expectedInstances[5].InstanceId), new Uri("https://dicom.unit.test/operations/" + expectedInstances[5].InstanceId)),
        };

        _durableClient
            .ListInstancesAsync(Arg.Is(GetPredicate(null)), source.Token)
            .Returns(new OrchestrationStatusQueryResult
            {
                ContinuationToken = "AAAA",
                DurableOrchestrationState = expectedInstances.Take(3).ToList(),
            });

        _durableClient
            .ListInstancesAsync(Arg.Is(GetPredicate("AAAA")), source.Token)
            .Returns(new OrchestrationStatusQueryResult
            {
                ContinuationToken = "BBBB",
                DurableOrchestrationState = expectedInstances.Skip(3).Take(2).ToList(),
            });

        _durableClient
            .ListInstancesAsync(Arg.Is(GetPredicate("BBBB")), source.Token)
            .Returns(new OrchestrationStatusQueryResult
            {
                ContinuationToken = null,
                DurableOrchestrationState = expectedInstances.Skip(5).ToList(),
            });

        foreach (OperationReference operation in expected)
        {
            _urlResolver.ResolveOperationStatusUri(operation.Id).Returns(operation.Href);
        }

        List<OperationReference> actual = await _client
            .FindOperationsAsync(
                new OperationQueryCondition<DicomOperation>
                {
                    CreatedTimeFrom = start,
                    CreatedTimeTo = end,
                    Operations = new DicomOperation[] { DicomOperation.Reindex },
                    Statuses = new OperationStatus[] { OperationStatus.NotStarted, OperationStatus.Running },
                },
                source.Token)
            .ToListAsync();

        await _durableClient
            .Received(1)
            .ListInstancesAsync(Arg.Is(GetPredicate(null)), source.Token);

        await _durableClient
            .Received(1)
            .ListInstancesAsync(Arg.Is(GetPredicate("AAAA")), source.Token);

        await _durableClient
            .Received(1)
            .ListInstancesAsync(Arg.Is(GetPredicate("BBBB")), source.Token);

        foreach (OperationReference operation in expected)
        {
            _urlResolver.Received(1).ResolveOperationStatusUri(operation.Id);
        }

        Assert.True(actual.Select(x => x.Id).SequenceEqual(expected.Select(x => x.Id)));
        Assert.True(actual.Select(x => x.Href).SequenceEqual(expected.Select(x => x.Href)));

        Expression<Predicate<OrchestrationStatusQueryCondition>> GetPredicate(string continuationToken)
            => (OrchestrationStatusQueryCondition x) =>
                x.ContinuationToken == continuationToken &&
                x.CreatedTimeFrom == start &&
                x.CreatedTimeTo == end &&
                x.InstanceIdPrefix == null &&
                x.ShowInput &&
                x.RuntimeStatus.SequenceEqual(
                    new OrchestrationRuntimeStatus[]
                    {
                        OrchestrationRuntimeStatus.Pending,
                        OrchestrationRuntimeStatus.Running,
                        OrchestrationRuntimeStatus.ContinuedAsNew
                    }) &&
                x.TaskHubNames == null;
    }

    [Fact]
    public async Task GivenNullTagKeys_WhenStartingReindex_ThenThrowArgumentNullException()
    {
        using var source = new CancellationTokenSource();

        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.StartReindexingInstancesAsync(Guid.NewGuid(), null, source.Token));

        await _durableClient.DidNotReceiveWithAnyArgs().StartNewAsync(default, default);
    }

    [Fact]
    public async Task GivenNoTagKeys_WhenStartingReindex_ThenThrowArgumentException()
    {
        using var source = new CancellationTokenSource();

        await Assert.ThrowsAsync<ArgumentException>(() => _client.StartReindexingInstancesAsync(Guid.NewGuid(), Array.Empty<int>(), source.Token));

        await _durableClient.DidNotReceiveWithAnyArgs().StartNewAsync(default, default);
    }

    [Fact]
    public async Task GivenAssignedKeys_WhenStartingReindex_ThenStartOrchestration()
    {
        string instanceId = OperationId.Generate();
        var operationId = Guid.Parse(instanceId);
        var tagKeys = new int[] { 10, 42 };
        var uri = new Uri("http://my-operation/" + operationId);

        using var source = new CancellationTokenSource();

        _durableClient
            .StartNewAsync(
                FunctionNames.ReindexInstances,
                instanceId,
                Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)))
            .Returns(instanceId);
        _urlResolver.ResolveOperationStatusUri(operationId).Returns(uri);

        OperationReference actual = await _client.StartReindexingInstancesAsync(operationId, tagKeys, source.Token);
        Assert.Equal(operationId, actual.Id);
        Assert.Equal(uri, actual.Href);

        await _durableClient
            .Received(1)
            .StartNewAsync(
                FunctionNames.ReindexInstances,
                instanceId,
                Arg.Is<ReindexInput>(x => x.QueryTagKeys.SequenceEqual(tagKeys)));
        _urlResolver.Received(1).ResolveOperationStatusUri(operationId);
    }

    [Fact]
    public async Task GivenNullArgs_WhenStartingExport_ThenThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.StartExportAsync(Guid.NewGuid(), null, new Uri("https://errors.log"), PartitionEntry.Default));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.StartExportAsync(Guid.NewGuid(), new ExportSpecification(), null, PartitionEntry.Default));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _client.StartExportAsync(Guid.NewGuid(), new ExportSpecification(), new Uri("https://errors.log"), null));
    }

    [Fact]
    public async Task GivenValidArgs_WhenStartingExport_ThenStartOrchestration()
    {
        var operationId = Guid.NewGuid();
        var spec = new ExportSpecification
        {
            Destination = new ExportDataOptions<ExportDestinationType>(ExportDestinationType.AzureBlob),
            Source = new ExportDataOptions<ExportSourceType>(ExportSourceType.Identifiers),
        };
        var errorHref = new Uri($"https://test.blob.core.windows.net/export/{operationId:N}/errors.log");
        var partition = new PartitionEntry(17, "test");
        var url = new Uri("http://foo.com/bar/operations/" + operationId.ToString(OperationId.FormatSpecifier));

        _durableClient
            .StartNewAsync(
                FunctionNames.ExportDicomFiles,
                operationId.ToString(OperationId.FormatSpecifier),
                Arg.Is<ExportInput>(x => ReferenceEquals(_options.Export.Batching, x.Batching)
                    && ReferenceEquals(spec.Destination, x.Destination)
                    && ReferenceEquals(errorHref, x.ErrorHref)
                    && ReferenceEquals(partition, x.Partition)
                    && ReferenceEquals(spec.Source, x.Source)))
            .Returns(operationId.ToString(OperationId.FormatSpecifier));
        _urlResolver
            .ResolveOperationStatusUri(operationId)
            .Returns(url);

        using var tokenSource = new CancellationTokenSource();
        OperationReference actual = await _client.StartExportAsync(operationId, spec, errorHref, partition, tokenSource.Token);

        await _durableClient
            .Received(1)
            .StartNewAsync(
                FunctionNames.ExportDicomFiles,
                operationId.ToString(OperationId.FormatSpecifier),
                Arg.Is<ExportInput>(x => ReferenceEquals(_options.Export.Batching, x.Batching)
                    && ReferenceEquals(spec.Destination, x.Destination)
                    && ReferenceEquals(errorHref, x.ErrorHref)
                    && ReferenceEquals(partition, x.Partition)
                    && ReferenceEquals(spec.Source, x.Source)));
        _urlResolver
            .Received(1)
            .ResolveOperationStatusUri(operationId);

        Assert.Equal(operationId, actual.Id);
        Assert.Equal(url, actual.Href);
    }
}
