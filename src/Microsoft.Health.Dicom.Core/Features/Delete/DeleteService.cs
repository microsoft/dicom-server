// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models.Delete;

namespace Microsoft.Health.Dicom.Core.Features.Delete;

public class DeleteService : IDeleteService
{
    private readonly IDicomRequestContextAccessor _contextAccessor;
    private readonly IIndexDataStore _indexDataStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IFileStore _fileStore;
    private readonly DeletedInstanceCleanupConfiguration _options;
    private readonly ITransactionHandler _transactionHandler;
    private readonly ILogger<DeleteService> _logger;
    private readonly bool _isExternalStoreEnabled;
    private readonly TelemetryClient _telemetryClient;

    private TimeSpan DeleteDelay => _isExternalStoreEnabled ? TimeSpan.Zero : _options.DeleteDelay;

    public DeleteService(
        IIndexDataStore indexDataStore,
        IMetadataStore metadataStore,
        IFileStore fileStore,
        IOptions<DeletedInstanceCleanupConfiguration> options,
        ITransactionHandler transactionHandler,
        ILogger<DeleteService> logger,
        IDicomRequestContextAccessor contextAccessor,
        IOptions<FeatureConfiguration> featureConfiguration,
        TelemetryClient telemetryClient)
    {
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _transactionHandler = EnsureArg.IsNotNull(transactionHandler, nameof(transactionHandler));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _contextAccessor = EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        _isExternalStoreEnabled = EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration)).EnableExternalStore;
        _telemetryClient = EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
    }

    public async Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken)
    {
        DateTimeOffset cleanupAfter = GenerateCleanupAfter(DeleteDelay);
        IReadOnlyCollection<VersionedInstanceIdentifier> identifiers = await _indexDataStore.DeleteStudyIndexAsync(GetPartition(), studyInstanceUid, cleanupAfter, cancellationToken);
        EmitTelemetry(identifiers);
    }

    public async Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
    {
        DateTimeOffset cleanupAfter = GenerateCleanupAfter(DeleteDelay);
        IReadOnlyCollection<VersionedInstanceIdentifier> identifiers = await _indexDataStore.DeleteSeriesIndexAsync(GetPartition(), studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
        EmitTelemetry(identifiers);
    }

    public async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
    {
        DateTimeOffset cleanupAfter = GenerateCleanupAfter(DeleteDelay);
        IReadOnlyCollection<VersionedInstanceIdentifier> identifiers = await _indexDataStore.DeleteInstanceIndexAsync(GetPartition(), studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        EmitTelemetry(identifiers);
    }

    public Task DeleteInstanceNowAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
    {
        return _indexDataStore.DeleteInstanceIndexAsync(GetPartition(), studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, cancellationToken);
    }

    public async Task<DeleteSummary> CleanUpDeletedInstancesAsync(CancellationToken cancellationToken)
    {
        int deletedCount = 0;
        IReadOnlyList<InstanceMetadata> candidates;
        using ITransactionScope transactionScope = _transactionHandler.BeginTransaction();

        try
        {
            candidates = await _indexDataStore.RetrieveDeletedInstancesWithPropertiesAsync(
                _options.BatchSize,
                _options.MaxRetries,
                cancellationToken);

            foreach (InstanceMetadata metadata in candidates)
            {
                if (!await TryDeleteInstanceDataAsync(metadata, cancellationToken))
                    deletedCount++;
            }
        }
        finally
        {
            transactionScope.Complete();
        }

        return new DeleteSummary
        {
            Found = candidates.Count,
            Deleted = deletedCount,
        };
    }

    public async Task<DeleteMetrics?> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        Task<DateTimeOffset> oldestWaitingToBeDeleted = _indexDataStore.GetOldestDeletedAsync(cancellationToken);
        Task<int> numReachedMaxedRetry = _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(
            _options.MaxRetries,
            cancellationToken);

        return new DeleteMetrics
        {
            OldestDeletion = await oldestWaitingToBeDeleted,
            TotalExhaustedRetries = await numReachedMaxedRetry,
        };
    }

    private void EmitTelemetry(IReadOnlyCollection<VersionedInstanceIdentifier> identifiers)
    {
        _logger.LogInformation("Instances queued for deletion: {Count}", identifiers.Count);
        _telemetryClient.ForwardLogTrace($"Instances queued for deletion: {identifiers.Count}");
        foreach (VersionedInstanceIdentifier identifier in identifiers)
        {
            _logger.LogInformation(
                "Instance queued for deletion. Instance Watermark: {Watermark} , PartitionKey: {PartitionKey} , ExternalStore: {ExternalStore}",
                identifier.Version, identifier.Partition.Key, _isExternalStoreEnabled);
            _telemetryClient.ForwardLogTrace("Instance queued for deletion", identifier);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are captured for success return value.")]
    private async Task<bool> TryDeleteInstanceDataAsync(InstanceMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {
            List<Task> tasks = new()
            {
                _fileStore.DeleteFileIfExistsAsync(
                    metadata.VersionedInstanceIdentifier.Version,
                    metadata.VersionedInstanceIdentifier.Partition,
                    metadata.InstanceProperties.FileProperties,
                    cancellationToken),
                _metadataStore.DeleteInstanceMetadataIfExistsAsync(
                    metadata.VersionedInstanceIdentifier.Version,
                    cancellationToken),
                _metadataStore.DeleteInstanceFramesRangeAsync(
                    metadata.VersionedInstanceIdentifier.Version,
                    cancellationToken)
            };

            // NOTE: in the input candidates we're going to have a row for each version in IDP,
            // but for non-IDP we'll have a single row whose original version needs to be explicitly candidates below.
            // To that end, we only need to delete by "original watermark" to catch changes from Update operation if not IDP.
            if (!_isExternalStoreEnabled && metadata.InstanceProperties.OriginalVersion.HasValue)
            {
                tasks.Add(_fileStore.DeleteFileIfExistsAsync(metadata.InstanceProperties.OriginalVersion.Value, metadata.VersionedInstanceIdentifier.Partition, metadata.InstanceProperties.FileProperties, cancellationToken));
                tasks.Add(_metadataStore.DeleteInstanceMetadataIfExistsAsync(metadata.InstanceProperties.OriginalVersion.Value, cancellationToken));
            }

            await Task.WhenAll(tasks);
            await _indexDataStore.DeleteDeletedInstanceAsync(metadata.VersionedInstanceIdentifier, cancellationToken);
        }
        catch (Exception cleanupException)
        {
            try
            {
                int newRetryCount = await _indexDataStore.IncrementDeletedInstanceRetryAsync(metadata.VersionedInstanceIdentifier, GenerateCleanupAfter(_options.RetryBackOff), cancellationToken);
                if (newRetryCount > _options.MaxRetries)
                {
                    _logger.LogCritical(cleanupException, "Failed to cleanup instance {DeletedInstanceIdentifier}. Retry count is now {NewRetryCount} and retry will not be re-attempted.", metadata, newRetryCount);
                }
                else
                {
                    _logger.LogError(cleanupException, "Failed to cleanup instance {DeletedInstanceIdentifier}. Retry count is now {NewRetryCount}.", metadata, newRetryCount);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Failed to increment cleanup retry for instance {DeletedInstanceIdentifier}.", metadata);
                return false;
            }
        }

        return true; // If we failed, but there are still retries left, we'll return true for now
    }

    private static DateTimeOffset GenerateCleanupAfter(TimeSpan delay)
        => Clock.UtcNow + delay;

    private Partition GetPartition()
        => _contextAccessor.RequestContext.GetPartition();
}
