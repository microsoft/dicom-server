// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Dicom.Functions.Client.Extensions;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill;
using Microsoft.Health.Dicom.Functions.DataCleanup;
using Microsoft.Health.Dicom.Functions.Export;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Update;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Client;

/// <summary>
/// Represents a client for interacting with DICOM-specific Azure Functions.
/// </summary>
internal class DicomAzureFunctionsClient : IDicomOperationsClient
{
    private readonly IDurableClient _durableClient;
    private readonly IUrlResolver _urlResolver;
    private readonly IDicomOperationsResourceStore _resourceStore;
    private readonly DicomFunctionOptions _options;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DicomAzureFunctionsClient"/> class.
    /// </summary>
    /// <param name="durableClientFactory">The client for interacting with durable functions.</param>
    /// <param name="urlResolver">A helper for building URLs for other APIs.</param>
    /// <param name="resourceStore">A store for resolving DICOM resources that are the subject of operations.</param>
    /// <param name="options">Options for configuring the functions.</param>
    /// <param name="jsonSerializerOptions">Json serialization options</param>
    /// <param name="logger">A logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="durableClientFactory"/>, <paramref name="urlResolver"/>, or
    /// <paramref name="resourceStore"/> is <see langword="null"/>.
    /// </exception>
    public DicomAzureFunctionsClient(
        IDurableClientFactory durableClientFactory,
        IUrlResolver urlResolver,
        IDicomOperationsResourceStore resourceStore,
        IOptions<DicomFunctionOptions> options,
        IOptions<JsonSerializerOptions> jsonSerializerOptions,
        ILogger<DicomAzureFunctionsClient> logger)
    {
        _durableClient = EnsureArg.IsNotNull(durableClientFactory, nameof(durableClientFactory)).CreateClient();
        _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        _resourceStore = EnsureArg.IsNotNull(resourceStore, nameof(resourceStore));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _jsonSerializerOptions = EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    public Task<IOperationState<DicomOperation>> GetStateAsync(Guid operationId, CancellationToken cancellationToken = default)
        => GetStateAsync<IOperationState<DicomOperation>>(
            operationId,
            async (operation, state, checkpoint, token) =>
            {
                OperationStatus status = state.RuntimeStatus.ToOperationStatus();
                return new OperationState<DicomOperation, object>
                {
                    CreatedTime = checkpoint.CreatedTime ?? state.CreatedTime,
                    LastUpdatedTime = state.LastUpdatedTime,
                    OperationId = operationId,
                    PercentComplete = checkpoint.PercentComplete.HasValue && status == OperationStatus.Succeeded ? 100 : checkpoint.PercentComplete,
                    Resources = await GetResourceUrlsAsync(operation, checkpoint.ResourceIds, cancellationToken),
                    Results = checkpoint.GetResults(state.Output),
                    Status = status,
                    Type = operation,
                };
            },
            cancellationToken);

    /// <inheritdoc/>
    public async IAsyncEnumerable<OperationReference> FindOperationsAsync(OperationQueryCondition<DicomOperation> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(query, nameof(query));

        var operations = query.Operations == null ? null : new HashSet<DicomOperation>(query.Operations);
        var result = new OrchestrationStatusQueryResult();
        do
        {
            result = await _durableClient.ListInstancesAsync(
                query.ForDurableFunctions(result.ContinuationToken),
                cancellationToken);

            IEnumerable<Guid> matches = GetValidOperationIds(result
                .DurableOrchestrationState
                .Where(s => operations == null || operations.Contains(s.GetDicomOperation())));

            foreach (Guid operationId in matches)
            {
                yield return new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));
            }
        } while (result.ContinuationToken != null && !cancellationToken.IsCancellationRequested);
    }

    /// <inheritdoc/>
    public Task<OperationCheckpointState<DicomOperation>> GetLastCheckpointAsync(Guid operationId, CancellationToken cancellationToken = default)
        => GetStateAsync(
            operationId,
            (operation, state, checkpoint, token) =>
                Task.FromResult(new OperationCheckpointState<DicomOperation>
                {
                    OperationId = operationId,
                    Status = state.RuntimeStatus.ToOperationStatus(),
                    Type = operation,
                    Checkpoint = checkpoint
                }),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<OperationReference> StartReindexingInstancesAsync(Guid operationId, IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
        EnsureArg.HasItems(tagKeys, nameof(tagKeys));

        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Pass token when supported
        string instanceId = await _durableClient.StartNewAsync(
            _options.Indexing.Name,
            operationId.ToString(OperationId.FormatSpecifier),
            new ReindexInput
            {
                Batching = _options.Indexing.Batching,
                QueryTagKeys = tagKeys
            });

        _logger.LogInformation("Successfully started new re-index orchestration instance with ID '{InstanceId}'.", instanceId);

        return new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));
    }

    /// <inheritdoc/>
    public async Task<OperationReference> StartExportAsync(Guid operationId, ExportSpecification specification, Uri errorHref, Partition partition, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(specification, nameof(specification));
        EnsureArg.IsNotNull(errorHref, nameof(errorHref));
        EnsureArg.IsNotNull(partition, nameof(partition));

        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Pass token when supported
        string instanceId = await _durableClient.StartNewAsync(
            _options.Export.Name,
            operationId.ToString(OperationId.FormatSpecifier),
            new ExportInput
            {
                Batching = _options.Export.Batching,
                Destination = specification.Destination,
                ErrorHref = errorHref,
                Partition = partition,
                Source = specification.Source,
            });

        _logger.LogInformation("Successfully started new export orchestration instance with ID '{InstanceId}'.", instanceId);

        return new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));
    }

    /// <inheritdoc/>
    public async Task<OperationReference> StartUpdateOperationAsync(Guid operationId, UpdateSpecification updateSpecification, Partition partition, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(updateSpecification, nameof(updateSpecification));

        cancellationToken.ThrowIfCancellationRequested();

        string datasetToUpdate = JsonSerializer.Serialize(updateSpecification.ChangeDataset, _jsonSerializerOptions);
        string instanceId = await _durableClient.StartNewAsync(
            _options.Update.Name,
            operationId.ToString(OperationId.FormatSpecifier),
            new UpdateInput
            {
                Partition = partition,
                ChangeDataset = datasetToUpdate,
                StudyInstanceUids = updateSpecification.StudyInstanceUids,
            });

        _logger.LogInformation("Successfully started new update operation instance with ID '{InstanceId}'.", instanceId);

        return new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));
    }

    /// <inheritdoc/>
    public async Task StartInstanceDataCleanupOperationAsync(Guid operationId, DateTimeOffset startFilterTimeStamp, DateTimeOffset endFilterTimeStamp, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsGt(endFilterTimeStamp, startFilterTimeStamp, nameof(endFilterTimeStamp));

        cancellationToken.ThrowIfCancellationRequested();

        string instanceId = await _durableClient.StartNewAsync(
            _options.DataCleanup.Name,
            operationId.ToString(OperationId.FormatSpecifier),
            new DataCleanupCheckPoint
            {
                Batching = _options.DataCleanup.Batching,
                StartFilterTimeStamp = startFilterTimeStamp,
                EndFilterTimeStamp = endFilterTimeStamp,
            });

        _logger.LogInformation("Successfully started data cleanup operation with ID '{InstanceId}'.", instanceId);
    }

    /// <inheritdoc/>
    public async Task StartContentLengthBackFillOperationAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string instanceId = await _durableClient.StartNewAsync(
            _options.ContentLengthBackFill.Name,
            operationId.ToString(OperationId.FormatSpecifier),
            new ContentLengthBackFillCheckPoint() { Batching = _options.ContentLengthBackFill.Batching });

        _logger.LogInformation("Successfully started content length backfill operation with ID '{InstanceId}'.", instanceId);
    }

    private async Task<T> GetStateAsync<T>(
        Guid operationId,
        Func<DicomOperation, DurableOrchestrationStatus, IOrchestrationCheckpoint, CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        DurableOrchestrationStatus state = await _durableClient.GetStatusAsync(operationId.ToString(OperationId.FormatSpecifier), showInput: true);

        if (state == null)
        {
            return null;
        }

        _logger.LogInformation(
            "Successfully found the state of orchestration instance '{InstanceId}' with name '{Name}'.",
            state.InstanceId,
            state.Name);

        DicomOperation type = state.GetDicomOperation();
        if (type == DicomOperation.Unknown)
        {
            _logger.LogWarning("Orchestration instance with '{Name}' did not resolve to a public operation type.", state.Name);
            return null;
        }

        return await factory(type, state, ParseCheckpoint(type, state), cancellationToken);
    }

    private async Task<IReadOnlyCollection<Uri>> GetResourceUrlsAsync(
        DicomOperation type,
        IReadOnlyCollection<string> resourceIds,
        CancellationToken cancellationToken)
    {
        switch (type)
        {
            case DicomOperation.Export:
            case DicomOperation.DataCleanup:
            case DicomOperation.ContentLengthBackFill:
                return null;
            case DicomOperation.Reindex:
                IReadOnlyList<Uri> tagPaths = Array.Empty<Uri>();
                List<int> tagKeys = resourceIds?.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToList();
                if (tagKeys?.Count > 0)
                {
                    tagPaths = await _resourceStore
                        .ResolveQueryTagKeysAsync(tagKeys, cancellationToken)
                        .Select(x => _urlResolver.ResolveQueryTagUri(x))
                        .ToListAsync(cancellationToken);
                }

                return tagPaths;
            case DicomOperation.Update:
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    // Note that the Durable Task Framework does not preserve the original CreatedTime
    // when an orchestration is restarted via ContinueAsNew, so we may store the original
    // in the checkpoint
    private static IOrchestrationCheckpoint ParseCheckpoint(DicomOperation type, DurableOrchestrationStatus status)
        => type switch
        {
            DicomOperation.DataCleanup => status.Input?.ToObject<DataCleanupCheckPoint>() ?? new DataCleanupCheckPoint(),
            DicomOperation.ContentLengthBackFill => status.Input?.ToObject<ContentLengthBackFillCheckPoint>() ?? new ContentLengthBackFillCheckPoint(),
            DicomOperation.Export => status.Input?.ToObject<ExportCheckpoint>() ?? new ExportCheckpoint(),
            DicomOperation.Reindex => status.Input?.ToObject<ReindexCheckpoint>() ?? new ReindexCheckpoint(),
            DicomOperation.Update => status.Input?.ToObject<UpdateCheckpoint>() ?? new UpdateCheckpoint(),
            _ => NullOrchestrationCheckpoint.Value,
        };

    private static IEnumerable<Guid> GetValidOperationIds(IEnumerable<DurableOrchestrationStatus> statuses)
    {
        foreach (DurableOrchestrationStatus status in statuses)
        {
            if (Guid.TryParse(status.InstanceId, out Guid operationId))
                yield return operationId;
        }
    }
}
