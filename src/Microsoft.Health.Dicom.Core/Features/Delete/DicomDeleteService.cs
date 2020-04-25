// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Delete
{
    public class DicomDeleteService : IDicomDeleteService
    {
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;
        private readonly IDicomFileStore _dicomFileStore;
        private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
        private readonly ITransactionHandler _transactionHandler;
        private readonly ILogger<DicomDeleteService> _logger;

        public DicomDeleteService(
            IDicomIndexDataStore dicomIndexDataStore,
            IDicomMetadataStore dicomMetadataStore,
            IDicomFileStore dicomFileStore,
            IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
            ITransactionHandler transactionHandler,
            ILogger<DicomDeleteService> logger)
        {
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));
            EnsureArg.IsNotNull(dicomFileStore, nameof(dicomFileStore));
            EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
            EnsureArg.IsNotNull(transactionHandler, nameof(transactionHandler));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomIndexDataStore = dicomIndexDataStore;
            _dicomMetadataStore = dicomMetadataStore;
            _dicomFileStore = dicomFileStore;
            _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
            _transactionHandler = transactionHandler;
            _logger = logger;
        }

        public async Task DeleteStudyAsync(string studyInstanceUid, CancellationToken cancellationToken)
        {
            DateTimeOffset cleanupAfter = GenerateCleanupAfter();
            await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            DateTimeOffset cleanupAfter = GenerateCleanupAfter();
            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            DateTimeOffset cleanupAfter = GenerateCleanupAfter();
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task<(bool success, int retrievedInstanceCount)> CleanupDeletedInstancesAsync(CancellationToken cancellationToken)
        {
            bool success = true;
            int retrievedInstanceCount = 0;

            using (ITransactionScope transactionScope = _transactionHandler.BeginTransaction())
            {
                try
                {
                    List<VersionedDicomInstanceIdentifier> deletedInstanceIdentifiers = (await _dicomIndexDataStore.RetrieveDeletedInstancesAsync(
                        _deletedInstanceCleanupConfiguration.BatchSize,
                        _deletedInstanceCleanupConfiguration.MaxRetries,
                        cancellationToken))
                        .ToList();

                    retrievedInstanceCount = deletedInstanceIdentifiers.Count;

                    foreach (VersionedDicomInstanceIdentifier deletedInstanceIdentifier in deletedInstanceIdentifiers)
                    {
                        try
                        {
                            Task[] tasks = new[]
                            {
                                _dicomFileStore.DeleteFileIfExistsAsync(deletedInstanceIdentifier, cancellationToken),
                                _dicomMetadataStore.DeleteInstanceMetadataIfExistsAsync(deletedInstanceIdentifier, cancellationToken),
                            };

                            await Task.WhenAll(tasks);

                            await _dicomIndexDataStore.DeleteDeletedInstanceAsync(deletedInstanceIdentifier, cancellationToken);
                        }
                        catch (Exception cleanupException)
                        {
                            try
                            {
                                int newRetryCount = await _dicomIndexDataStore.IncrementDeletedInstanceRetryAsync(deletedInstanceIdentifier, GenerateCleanupAfter(), cancellationToken);
                                if (newRetryCount > _deletedInstanceCleanupConfiguration.MaxRetries)
                                {
                                    _logger.LogCritical(cleanupException, $"Failed to cleanup instance {deletedInstanceIdentifier}. Retry count is now {newRetryCount} and retry will not be re-attempted.");
                                }
                                else
                                {
                                    _logger.LogError(cleanupException, $"Failed to cleanup instance {deletedInstanceIdentifier}. Retry count is now {newRetryCount}.");
                                }
                            }
                            catch (Exception incrementException)
                            {
                                _logger.LogCritical(incrementException, $"Failed to increment cleanup retry for instance {deletedInstanceIdentifier}.");
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

        private DateTimeOffset GenerateCleanupAfter()
        {
            return Clock.UtcNow + _deletedInstanceCleanupConfiguration.DeleteDelay;
        }
    }
}
