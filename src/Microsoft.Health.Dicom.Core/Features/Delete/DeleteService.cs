// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Delete
{
    public class DeleteService : IDeleteService
    {
        private readonly IDicomRequestContextAccessor _contextAccessor;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IFileStore _fileStore;
        private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
        private readonly ITransactionHandler _transactionHandler;
        private readonly ILogger<DeleteService> _logger;

        public DeleteService(
            IIndexDataStore indexDataStore,
            IMetadataStore metadataStore,
            IFileStore fileStore,
            IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
            ITransactionHandler transactionHandler,
            ILogger<DeleteService> logger,
            IDicomRequestContextAccessor contextAccessor)
        {
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
            EnsureArg.IsNotNull(fileStore, nameof(fileStore));
            EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
            EnsureArg.IsNotNull(transactionHandler, nameof(transactionHandler));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));

            _indexDataStore = indexDataStore;
            _metadataStore = metadataStore;
            _fileStore = fileStore;
            _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
            _transactionHandler = transactionHandler;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        public Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken)
        {
            DateTimeOffset cleanupAfter = GenerateCleanupAfter(_deletedInstanceCleanupConfiguration.DeleteDelay);
            return _indexDataStore.DeleteStudyIndexAsync(GetPartitionKey(), studyInstanceUid, cleanupAfter, cancellationToken);
        }

        public Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            DateTimeOffset cleanupAfter = GenerateCleanupAfter(_deletedInstanceCleanupConfiguration.DeleteDelay);
            return _indexDataStore.DeleteSeriesIndexAsync(GetPartitionKey(), studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
        }

        public Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            DateTimeOffset cleanupAfter = GenerateCleanupAfter(_deletedInstanceCleanupConfiguration.DeleteDelay);
            return _indexDataStore.DeleteInstanceIndexAsync(GetPartitionKey(), studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public Task DeleteInstanceNowAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            return _indexDataStore.DeleteInstanceIndexAsync(GetPartitionKey(), studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, cancellationToken);
        }

        public async Task<(bool success, int retrievedInstanceCount)> CleanupDeletedInstancesAsync(CancellationToken cancellationToken)
        {
            bool success = true;
            int retrievedInstanceCount = 0;

            using (ITransactionScope transactionScope = _transactionHandler.BeginTransaction())
            {
                try
                {
                    var deletedInstanceIdentifiers = (await _indexDataStore
                        .RetrieveDeletedInstancesAsync(
                            _deletedInstanceCleanupConfiguration.BatchSize,
                            _deletedInstanceCleanupConfiguration.MaxRetries,
                            cancellationToken))
                        .ToList();

                    retrievedInstanceCount = deletedInstanceIdentifiers.Count;

                    foreach (VersionedInstanceIdentifier deletedInstanceIdentifier in deletedInstanceIdentifiers)
                    {
                        try
                        {
                            Task[] tasks = new[]
                            {
                                _fileStore.DeleteFileIfExistsAsync(deletedInstanceIdentifier, cancellationToken),
                                _metadataStore.DeleteInstanceMetadataIfExistsAsync(deletedInstanceIdentifier, cancellationToken),
                            };

                            await Task.WhenAll(tasks);

                            await _indexDataStore.DeleteDeletedInstanceAsync(deletedInstanceIdentifier, cancellationToken);
                        }
                        catch (Exception cleanupException)
                        {
                            try
                            {
                                int newRetryCount = await _indexDataStore.IncrementDeletedInstanceRetryAsync(deletedInstanceIdentifier, GenerateCleanupAfter(_deletedInstanceCleanupConfiguration.RetryBackOff), cancellationToken);
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

        private static DateTimeOffset GenerateCleanupAfter(TimeSpan delay)
        {
            return Clock.UtcNow + delay;
        }

        private int GetPartitionKey()
        {
            return _contextAccessor.RequestContext.GetPartitionKey();
        }
    }
}
