// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides logging for <see cref="IIndexDataStore"/>.
    /// </summary>
    public class LoggingIndexDataStore : IIndexDataStore
    {
        private static readonly Action<ILogger, InstanceIdentifier, Exception> LogBeginAddInstanceDelegate =
            LoggerMessage.Define<InstanceIdentifier>(
                LogLevel.Debug,
                default,
                "Starting creation of DICOM instance index with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, InstanceIdentifier, Exception> LogReindexIndexDelegate =
                LoggerMessage.Define<InstanceIdentifier>(
                LogLevel.Debug,
                default,
                "Reindexing DICOM instance with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, long, Exception> LogCreateInstanceIndexSucceededDelegate =
            LoggerMessage.Define<long>(
                LogLevel.Debug,
                default,
                "The DICOM instance has been successfully created with version '{Version}'.");

        private static readonly Action<ILogger, string, string, string, DateTimeOffset, Exception> LogDeleteInstanceIndexDelegate =
            LoggerMessage.Define<string, string, string, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index with Study '{StudyInstanceUid}', Series '{SeriesInstanceUid}, and SopInstance '{SopInstanceUid}' to be cleaned up after '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, string, string, string, DateTimeOffset, Exception> LogDeleteInstanceIndexWithPartitionDelegate =
            LoggerMessage.Define<string, string, string, string, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index with Partition '{PartitionId}', Study '{StudyInstanceUid}', Series '{SeriesInstanceUid}, and SopInstance '{SopInstanceUid}' to be cleaned up after '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, string, DateTimeOffset, Exception> LogDeleteSeriesIndexDelegate =
            LoggerMessage.Define<string, string, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index within Study's Series with Study '{StudyInstanceUid}' and Series '{SeriesInstanceUid}' to be cleaned up after '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, string, string, DateTimeOffset, Exception> LogDeleteSeriesIndexWithPartitionDelegate =
            LoggerMessage.Define<string, string, string, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index within Study's Series with Partition '{PartitionId}', Study '{StudyInstanceUid}' and Series '{SeriesInstanceUid}' to be cleaned up after '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, DateTimeOffset, Exception> LogDeleteStudyInstanceIndexDelegate =
            LoggerMessage.Define<string, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index within Study with Study '{StudyInstanceUid}' to be cleaned up after '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, string, DateTimeOffset, Exception> LogDeleteStudyInstanceIndexWithPartitionDelegate =
            LoggerMessage.Define<string, string, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index within Study with Partition '{PartitionId}' and Study '{StudyInstanceUid}' to be cleaned up after '{CleanupAfter}'.");

        private static readonly Action<ILogger, VersionedInstanceIdentifier, Exception> LogEndAddInstanceDelegate =
            LoggerMessage.Define<VersionedInstanceIdentifier>(
                LogLevel.Debug,
                default,
                "Completing creation of DICOM instance with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, int, int, Exception> LogRetrieveDeletedInstancesAsyncDelegate =
            LoggerMessage.Define<int, int>(
                LogLevel.Debug,
                default,
                "Retrieving {BatchSize} deleted instances with less than or equal to {MaxRetries}.");

        private static readonly Action<ILogger, VersionedInstanceIdentifier, Exception> LogDeleteDeletedInstanceAsyncDelegate =
            LoggerMessage.Define<VersionedInstanceIdentifier>(
                LogLevel.Debug,
                default,
                "Removing deleted instance '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, VersionedInstanceIdentifier, DateTimeOffset, Exception> LogIncrementDeletedInstanceRetryAsyncDelegate =
            LoggerMessage.Define<VersionedInstanceIdentifier, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Incrementing the retry count of deleted instances '{DicomInstanceIdentifier}' and setting next cleanup time to '{CleanupAfter}'.");

        private static readonly Action<ILogger, Exception> LogGetOldestDeletedAsyncDelegate =
           LoggerMessage.Define(
               LogLevel.Debug,
               default,
               "Finding time of oldest deleted instance.");

        private static readonly Action<ILogger, int, Exception> LogRetrieveNumDeletedExceedRetryCountAsyncDelegate =
           LoggerMessage.Define<int>(
               LogLevel.Debug,
               default,
               "Finding number of delete instances at max retries of {MaxRetriesAllowed}.");

        private static readonly Action<ILogger, Exception> LogCheckIfInstancesExistAsyncDelegate =
           LoggerMessage.Define(
               LogLevel.Debug,
               default,
               "Checking if data exists that would conflict with data partition feature.");

        private static readonly Action<ILogger, Exception> LogOperationSucceededDelegate =
            LoggerMessage.Define(
                LogLevel.Debug,
                default,
                "The operation completed successfully.");

        private static readonly Action<ILogger, Exception> LogOperationFailedDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "The operation failed.");

        private readonly ILogger _logger;

        public LoggingIndexDataStore(IIndexDataStore indexDataStore, ILogger<LoggingIndexDataStore> logger)
        {
            EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            IndexDataStore = indexDataStore;
            _logger = logger;
        }

        protected IIndexDataStore IndexDataStore { get; }

        /// <inheritdoc />
        public async Task<long> BeginCreateInstanceIndexAsync(string partitionId, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            LogBeginAddInstanceDelegate(_logger, dicomDataset.ToInstanceIdentifier(partitionId), null);

            try
            {
                long version = await IndexDataStore.BeginCreateInstanceIndexAsync(partitionId, dicomDataset, queryTags, cancellationToken);

                LogCreateInstanceIndexSucceededDelegate(_logger, version, null);

                return version;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDataset);
            LogReindexIndexDelegate(_logger, dicomDataset.ToVersionedInstanceIdentifier(watermark), null);

            try
            {
                await IndexDataStore.ReindexInstanceAsync(dicomDataset, watermark, queryTags, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

            }
            catch (DataStoreException ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteInstanceIndexAsync(string partitionId, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(partitionId))
            {
                LogDeleteInstanceIndexDelegate(_logger, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, null);
            }
            else
            {
                LogDeleteInstanceIndexWithPartitionDelegate(_logger, partitionId, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, null);
            }

            try
            {
                await IndexDataStore.DeleteInstanceIndexAsync(partitionId, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteSeriesIndexAsync(string partitionId, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(partitionId))
            {
                LogDeleteSeriesIndexDelegate(_logger, studyInstanceUid, seriesInstanceUid, cleanupAfter, null);
            }
            else
            {
                LogDeleteSeriesIndexWithPartitionDelegate(_logger, partitionId, studyInstanceUid, seriesInstanceUid, cleanupAfter, null);
            }

            try
            {
                await IndexDataStore.DeleteSeriesIndexAsync(partitionId, studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteStudyIndexAsync(string partitionId, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(partitionId))
            {
                LogDeleteStudyInstanceIndexDelegate(_logger, studyInstanceUid, cleanupAfter, null);
            }
            else
            {
                LogDeleteStudyInstanceIndexWithPartitionDelegate(_logger, partitionId, studyInstanceUid, cleanupAfter, null);
            }

            try
            {
                await IndexDataStore.DeleteStudyIndexAsync(partitionId, studyInstanceUid, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task EndCreateInstanceIndexAsync(string partitionId, DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, bool allowExpiredTags = false, CancellationToken cancellationToken = default)
        {
            LogEndAddInstanceDelegate(_logger, dicomDataset.ToVersionedInstanceIdentifier(watermark), null);

            try
            {
                await IndexDataStore.EndCreateInstanceIndexAsync(partitionId, dicomDataset, watermark, queryTags, allowExpiredTags, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken)
        {
            LogRetrieveDeletedInstancesAsyncDelegate(_logger, batchSize, maxRetries, null);

            try
            {
                IEnumerable<VersionedInstanceIdentifier> deletedInstances = await IndexDataStore.RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return deletedInstances;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            LogDeleteDeletedInstanceAsyncDelegate(_logger, versionedInstanceIdentifier, null);

            try
            {
                await IndexDataStore.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            LogIncrementDeletedInstanceRetryAsyncDelegate(_logger, versionedInstanceIdentifier, cleanupAfter, null);

            try
            {
                int returnValue = await IndexDataStore.IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return returnValue;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken)
        {
            LogRetrieveNumDeletedExceedRetryCountAsyncDelegate(_logger, maxNumberOfRetries, null);

            try
            {
                int returnValue = await IndexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(maxNumberOfRetries, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return returnValue;
            }
            catch (DataStoreException ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        public async Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken)
        {
            LogGetOldestDeletedAsyncDelegate(_logger, null);

            try
            {
                DateTimeOffset returnValue = await IndexDataStore.GetOldestDeletedAsync(cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return returnValue;
            }
            catch (DataStoreException ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        public async Task CheckIfInstancesExistAsync(CancellationToken cancellationToken = default)
        {
            LogCheckIfInstancesExistAsyncDelegate(_logger, null);

            try
            {
                await IndexDataStore.CheckIfInstancesExistAsync(cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (DataStoreException ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }
    }
}
