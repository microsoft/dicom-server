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
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Extensions;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DicomAzureFunctionsClient"/> class.
    /// </summary>
    /// <param name="durableClientFactory">The client for interacting with durable functions.</param>
    /// <param name="urlResolver">A helper for building URLs for other APIs.</param>
    /// <param name="resourceStore">A store for resolving DICOM resources that are the subject of operations.</param>
    /// <param name="options">Options for configuring the functions.</param>
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
        ILogger<DicomAzureFunctionsClient> logger)
    {
        _durableClient = EnsureArg.IsNotNull(durableClientFactory, nameof(durableClientFactory)).CreateClient();
        _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        _resourceStore = EnsureArg.IsNotNull(resourceStore, nameof(resourceStore));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OperationState<DicomOperation>> GetStateAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // TODO: Pass token when supported
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

        OperationStatus status = state.RuntimeStatus.ToOperationStatus();
        IOperationCheckpoint checkpoint = ParseCheckpoint(type, state);
        return new OperationState<DicomOperation>
        {
            CreatedTime = checkpoint.CreatedTime ?? state.CreatedTime,
            LastUpdatedTime = state.LastUpdatedTime,
            OperationId = operationId,
            PercentComplete = checkpoint.PercentComplete.HasValue && status == OperationStatus.Completed ? 100 : checkpoint.PercentComplete,
            Resources = await GetResourceUrlsAsync(type, checkpoint.ResourceIds, cancellationToken),
            Status = status,
            Type = type,
        };
    }

    /// <inheritdoc/>
    public async Task<OperationReference> StartReindexingInstancesAsync(Guid operationId, IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
        EnsureArg.HasItems(tagKeys, nameof(tagKeys));

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
    public async Task<OperationReference> StartExportAsync(Guid operationId, ExportSpecification specification, PartitionEntry partition, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(specification, nameof(specification));
        EnsureArg.IsNotNull(partition, nameof(partition));

        // TODO: Pass token when supported
        string instanceId = await _durableClient.StartNewAsync(
            _options.Export.Name,
            operationId.ToString(OperationId.FormatSpecifier),
            new ExportInput
            {
                Batching = _options.Export.Batching,
                Destination = specification.Destination,
                Partition = partition,
                Source = specification.Source,
            });

        _logger.LogInformation("Successfully started new export orchestration instance with ID '{InstanceId}'.", instanceId);

        return new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));
    }

    /// <inheritdoc/>
    public Task<Guid> StartBlobCopyAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implementation is in different PR
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> IsBlobCopyCompletedAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implementation is in different PR
        throw new NotImplementedException();
    }

    // Note that the Durable Task Framework does not preserve the original CreatedTime
    // when an orchestration is restarted via ContinueAsNew, so we may store the original
    // in the checkpoint
    private static IOperationCheckpoint ParseCheckpoint(DicomOperation type, DurableOrchestrationStatus status)
        => type switch
        {
            DicomOperation.Export => status.Input?.ToObject<ExportCheckpoint>() ?? new ExportCheckpoint(),
            DicomOperation.Reindex => status.Input?.ToObject<ReindexCheckpoint>() ?? new ReindexCheckpoint(),
            _ => NullOperationCheckpoint.Value,
        };

    private async Task<IReadOnlyCollection<Uri>> GetResourceUrlsAsync(
        DicomOperation type,
        IReadOnlyCollection<string> resourceIds,
        CancellationToken cancellationToken)
    {
        switch (type)
        {
            case DicomOperation.Export:
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
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
