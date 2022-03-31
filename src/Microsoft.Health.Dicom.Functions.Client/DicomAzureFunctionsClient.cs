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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.DurableTask;
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
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IUrlResolver _urlResolver;
    private readonly IGuidFactory _guidFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DicomAzureFunctionsClient"/> class.
    /// </summary>
    /// <param name="durableClientFactory">The client for interacting with durable functions.</param>
    /// <param name="urlResolver">A helper for building URLs for other APIs.</param>
    /// <param name="extendedQueryTagStore">An extended query tag store for resolving the query tag IDs.</param>
    /// <param name="guidFactory">A factory for creating instances of <see cref="Guid"/>.</param>
    /// <param name="logger">A logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="durableClientFactory"/>, <paramref name="urlResolver"/>,
    /// <paramref name="extendedQueryTagStore"/>, <paramref name="guidFactory"/> is <see langword="null"/>.
    /// </exception>
    public DicomAzureFunctionsClient(
        IDurableClientFactory durableClientFactory,
        IExtendedQueryTagStore extendedQueryTagStore,
        IUrlResolver urlResolver,
        IGuidFactory guidFactory,
        ILogger<DicomAzureFunctionsClient> logger)
    {
        _durableClient = EnsureArg.IsNotNull(durableClientFactory, nameof(durableClientFactory)).CreateClient();
        _urlResolver = EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
        _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
        _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
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
            PercentComplete = status == OperationStatus.Completed ? 100 : checkpoint.PercentComplete,
            Resources = await GetResourceUrlsAsync(type, checkpoint.ResourceIds, cancellationToken),
            Status = status,
            Type = type,
        };
    }

    /// <inheritdoc/>
    public async Task<Guid> StartReindexingInstancesAsync(IReadOnlyCollection<int> tagKeys, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
        EnsureArg.HasItems(tagKeys, nameof(tagKeys));

        // Start the re-indexing orchestration
        Guid operationId = _guidFactory.Create();

        // TODO: Pass token when supported
        string instanceId = await _durableClient.StartNewAsync(
            FunctionNames.ReindexInstances,
            operationId.ToString(OperationId.FormatSpecifier),
            new ReindexInput { QueryTagKeys = tagKeys });

        _logger.LogInformation("Successfully started new orchestration instance with ID '{InstanceId}'.", instanceId);

        // Associate the tags to the operation and confirm their processing
        IReadOnlyList<ExtendedQueryTagStoreEntry> confirmedTags = await _extendedQueryTagStore.AssignReindexingOperationAsync(
            tagKeys,
            operationId,
            returnIfCompleted: true,
            cancellationToken: cancellationToken);

        return confirmedTags.Count > 0 ? operationId : throw new ExtendedQueryTagsAlreadyExistsException();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public async Task<Guid> StartExportAsync(ExportInput input, CancellationToken cancellationToken)
    {
        // Start the export orchestration
        Guid operationId = _guidFactory.Create();

        // TODO: Pass token when supported
        string instanceId = await _durableClient.StartNewAsync(
            FunctionNames.Export,
            operationId.ToString(OperationId.FormatSpecifier),
            input);

        _logger.LogInformation("Successfully started new orchestration instance with ID '{InstanceId}'.", instanceId);
        return operationId;
    }

    // Note that the Durable Task Framework does not preserve the original CreatedTime
    // when an orchestration is restarted via ContinueAsNew, so we may store the original
    // in the checkpoint
    private static IOperationCheckpoint ParseCheckpoint(DicomOperation type, DurableOrchestrationStatus status)
        => type switch
        {
            // TODO: Pass serializer options
            DicomOperation.Export => status.Input?.ToObject<ExportCheckpoint>() ?? new ExportCheckpoint(),
            DicomOperation.Reindex => status.Input?.ToObject<ReindexInput>() ?? new ReindexInput(),
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
                List<int> tagKeys = resourceIds?.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToList();

                IReadOnlyCollection<ExtendedQueryTagStoreEntry> tagPaths = Array.Empty<ExtendedQueryTagStoreEntry>();
                if (tagKeys?.Count > 0)
                {
                    tagPaths = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagKeys, cancellationToken);
                }

                return tagPaths.Select(x => _urlResolver.ResolveQueryTagUri(x.Path)).ToList();
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
