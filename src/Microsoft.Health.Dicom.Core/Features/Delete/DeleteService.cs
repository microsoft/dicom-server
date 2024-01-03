// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
#if !NET8_0_OR_GREATER
using Microsoft.Health.Core;
#endif
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
    private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
    private readonly ITransactionHandler _transactionHandler;
    private readonly ILogger<DeleteService> _logger;
    private readonly bool _isExternalStoreEnabled;
    private readonly TelemetryClient _telemetryClient;

    private TimeSpan DeleteDelay => _isExternalStoreEnabled ? TimeSpan.Zero : _deletedInstanceCleanupConfiguration.DeleteDelay;

#if NET8_0_OR_GREATER
    private readonly TimeProvider _timeProvider;

    public DeleteService(
        IIndexDataStore indexDataStore,
        IMetadataStore metadataStore,
        IFileStore fileStore,
        IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
        ITransactionHandler transactionHandler,
        ILogger<DeleteService> logger,
        IDicomRequestContextAccessor contextAccessor,
        IOptions<FeatureConfiguration> featureConfiguration,
        TelemetryClient telemetryClient)
        : this(
            indexDataStore,
            metadataStore,
            fileStore,
            deletedInstanceCleanupConfiguration,
            transactionHandler,
            logger,
            contextAccessor,
            featureConfiguration,
            telemetryClient,
            TimeProvider.System)
    { }

    internal DeleteService(
        IIndexDataStore indexDataStore,
        IMetadataStore metadataStore,
        IFileStore fileStore,
        IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
        ITransactionHandler transactionHandler,
        ILogger<DeleteService> logger,
        IDicomRequestContextAccessor contextAccessor,
        IOptions<FeatureConfiguration> featureConfiguration,
        TelemetryClient telemetryClient,
        TimeProvider timeProvider)
    {
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));
#else
    public DeleteService(
        IIndexDataStore indexDataStore,
        IMetadataStore metadataStore,
        IFileStore fileStore,
        IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
        ITransactionHandler transactionHandler,
        ILogger<DeleteService> logger,
        IDicomRequestContextAccessor contextAccessor,
        IOptions<FeatureConfiguration> featureConfiguration,
        TelemetryClient telemetryClient)
    {
#endif
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _fileStore = EnsureArg.IsNotNull(fileStore, nameof(fileStore));
        _deletedInstanceCleanupConfiguration = EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
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
        return _indexDataStore.DeleteInstanceIndexAsync(
            GetPartition(),
            studyInstanceUid,
            seriesInstanceUid,
            sopInstanceUid,
#if NET8_0_OR_GREATER
            _timeProvider.GetUtcNow(),
#else
            Clock.UtcNow,
#endif
            cancellationToken);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are captured for success return value.")]
    public async Task<(bool Success, int RetrievedInstanceCount)> CleanupDeletedInstancesAsync(CancellationToken cancellationToken)
    {
        bool success = true;
        int retrievedInstanceCount = 0;

        using (ITransactionScope transactionScope = _transactionHandler.BeginTransaction())
        {
            try
            {
                var deletedInstanceIdentifiers = (await _indexDataStore
                    .RetrieveDeletedInstancesWithPropertiesAsync(
                        _deletedInstanceCleanupConfiguration.BatchSize,
                        _deletedInstanceCleanupConfiguration.MaxRetries,
                        cancellationToken))
                    .ToList();

                retrievedInstanceCount = deletedInstanceIdentifiers.Count;

                foreach (InstanceMetadata deletedInstanceIdentifier in deletedInstanceIdentifiers)
                {
                    try
                    {
                        List<Task> tasks = new List<Task>()
                        {
                            _fileStore.DeleteFileIfExistsAsync(
                                deletedInstanceIdentifier.VersionedInstanceIdentifier.Version,
                                deletedInstanceIdentifier.VersionedInstanceIdentifier.Partition,
                                deletedInstanceIdentifier.InstanceProperties.FileProperties,
                                cancellationToken),
                            _metadataStore.DeleteInstanceMetadataIfExistsAsync(
                                deletedInstanceIdentifier.VersionedInstanceIdentifier.Version,
                                cancellationToken),
                            _metadataStore.DeleteInstanceFramesRangeAsync(
                                deletedInstanceIdentifier.VersionedInstanceIdentifier.Version,
                                cancellationToken)
                        };

                        // NOTE: in the input deletedInstanceIdentifiers we're going to have a row for each version in IDP,
                        // but for non-IDP we'll have a single row whose original version needs to be explicitly deleted below.
                        // To that end, we only need to delete by "original watermark" to catch changes from Update operation if not IDP.
                        if (!_isExternalStoreEnabled && deletedInstanceIdentifier.InstanceProperties.OriginalVersion.HasValue)
                        {
                            tasks.Add(_fileStore.DeleteFileIfExistsAsync(deletedInstanceIdentifier.InstanceProperties.OriginalVersion.Value, deletedInstanceIdentifier.VersionedInstanceIdentifier.Partition, deletedInstanceIdentifier.InstanceProperties.FileProperties, cancellationToken));
                            tasks.Add(_metadataStore.DeleteInstanceMetadataIfExistsAsync(deletedInstanceIdentifier.InstanceProperties.OriginalVersion.Value, cancellationToken));
                        }

                        await Task.WhenAll(tasks);

                        await _indexDataStore.DeleteDeletedInstanceAsync(deletedInstanceIdentifier.VersionedInstanceIdentifier, cancellationToken);
                    }
                    catch (Exception cleanupException)
                    {
                        try
                        {
                            int newRetryCount = await _indexDataStore.IncrementDeletedInstanceRetryAsync(deletedInstanceIdentifier.VersionedInstanceIdentifier, GenerateCleanupAfter(_deletedInstanceCleanupConfiguration.RetryBackOff), cancellationToken);
                            if (newRetryCount > _deletedInstanceCleanupConfiguration.MaxRetries)
                            {
                                _logger.LogCritical(cleanupException, "Failed to cleanup instance {DeletedInstanceIdentifier}. Retry count is now {NewRetryCount} and retry will not be re-attempted.", deletedInstanceIdentifier, newRetryCount);
                            }
                            else
                            {
                                _logger.LogError(cleanupException, "Failed to cleanup instance {DeletedInstanceIdentifier}. Retry count is now {NewRetryCount}.", deletedInstanceIdentifier, newRetryCount);
                            }
                        }
                        catch (Exception incrementException)
                        {
                            _logger.LogCritical(incrementException, "Failed to increment cleanup retry for instance {DeletedInstanceIdentifier}.", deletedInstanceIdentifier);
                            success = false;
                        }
                    }
                }

                transactionScope.Complete();
            }
            catch (Exception retrieveException)
            {
                _logger.LogCritical(retrieveException, "Failed to retrieve instances to cleanup.");
                success = false;
            }
        }

        return (success, retrievedInstanceCount);
    }

    public async Task<DeleteMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        Task<DateTimeOffset> oldestWaitingToBeDeleted = _indexDataStore.GetOldestDeletedAsync(cancellationToken);
        Task<int> numReachedMaxedRetry = _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(
            _deletedInstanceCleanupConfiguration.MaxRetries,
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
        foreach (var identifier in identifiers)
        {
            _logger.LogInformation(
                "Instance queued for deletion. Instance Watermark: {Watermark} , PartitionKey: {PartitionKey} , ExternalStore: {ExternalStore}",
                identifier.Version, identifier.Partition.Key, _isExternalStoreEnabled);
            _telemetryClient.ForwardLogTrace("Instance queued for deletion", identifier);
        }
    }

    private Partition GetPartition()
        => _contextAccessor.RequestContext.GetPartition();

#if NET8_0_OR_GREATER
    private DateTimeOffset GenerateCleanupAfter(TimeSpan delay)
        => _timeProvider.GetUtcNow() + delay;
#else
    private static DateTimeOffset GenerateCleanupAfter(TimeSpan delay)
        => Clock.UtcNow + delay;
#endif
}
