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
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
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
            await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, cancellationToken);
        }

        public async Task DeleteSeriesAsync(string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cancellationToken);
        }

        public async Task DeleteInstanceAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken)
        {
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
        }

        public async Task<(bool success, int rowsProcessed)> CleanupDeletedInstancesAsync(CancellationToken cancellationToken = default)
        {
            bool success = true;
            int rowsProcessed = 0;

            using (ITransactionScope transactionScope = _transactionHandler.BeginTransaction())
            {
                try
                {
                    var deletedInstanceIdentifiers = (await _dicomIndexDataStore.RetrieveDeletedInstancesAsync(
                        _deletedInstanceCleanupConfiguration.DeleteDelay,
                        _deletedInstanceCleanupConfiguration.BatchSize,
                        _deletedInstanceCleanupConfiguration.MaxRetries,
                        cancellationToken))
                        .ToList();

                    rowsProcessed = deletedInstanceIdentifiers.Count;

                    foreach (VersionedDicomInstanceIdentifier deletedInstanceIdentifier in deletedInstanceIdentifiers)
                    {
                        try
                        {
                            await _dicomFileStore.DeleteIfExistsAsync(deletedInstanceIdentifier, cancellationToken);
                            await _dicomMetadataStore.DeleteInstanceMetadataIfExistsAsync(deletedInstanceIdentifier, cancellationToken);
                            await _dicomIndexDataStore.DeleteDeletedInstanceAsync(deletedInstanceIdentifier, cancellationToken);
                        }
                        catch (Exception cleanupException)
                        {
                            _logger.LogError(cleanupException, "Failed to cleanup instance.");

                            try
                            {
                                await _dicomIndexDataStore.IncrementDeletedInstanceRetryAsync(deletedInstanceIdentifier, _deletedInstanceCleanupConfiguration.RetryBackOff, cancellationToken);
                            }
                            catch (Exception incrementException)
                            {
                                _logger.LogCritical(incrementException, "Failed to increment cleanup retry.");
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

            return (success, rowsProcessed);
        }
    }
}
