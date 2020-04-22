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
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class LoggingDicomIndexDataStore : IDicomIndexDataStore
    {
        private static readonly Action<ILogger, DicomInstanceIdentifier, Exception> LogCreateInstanceIndexDelegate =
            LoggerMessage.Define<DicomInstanceIdentifier>(
                LogLevel.Debug,
                default,
                "Creating DICOM instance index with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, long, Exception> LogCreateInstanceIndexSucceededDelegate =
            LoggerMessage.Define<long>(
                LogLevel.Debug,
                default,
                "The DICOM instance has been successfully created with version '{Version}'.");

        private static readonly Action<ILogger, string, string, string, DateTimeOffset, DateTimeOffset, Exception> LogDeleteInstanceIndexDelegate =
            LoggerMessage.Define<string, string, string, DateTimeOffset, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index with Study '{StudyInstanceUid}', Series '{SeriesInstanceUid}, and SopInstance '{SopInstanceUid}' with a date of {DeletedDate} and to be cleanedUpAfter '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, string, DateTimeOffset, DateTimeOffset, Exception> LogDeleteSeriesIndexDelegate =
            LoggerMessage.Define<string, string, DateTimeOffset, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index within Study's Series with Study '{StudyInstanceUid}' and Series '{SeriesInstanceUid}' with a date of {DeletedDate} and to be cleanedUpAfter '{CleanupAfter}'.");

        private static readonly Action<ILogger, string, DateTimeOffset, DateTimeOffset, Exception> LogDeleteStudyInstanceIndexDelegate =
            LoggerMessage.Define<string, DateTimeOffset, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance index within Study with Study '{StudyInstanceUid}' with a date of {DeletedDate} and to be cleanedUpAfter '{CleanupAfter}'.");

        private static readonly Action<ILogger, VersionedDicomInstanceIdentifier, DicomIndexStatus, Exception> LogUpdateInstanceIndexStatusDelegate =
            LoggerMessage.Define<VersionedDicomInstanceIdentifier, DicomIndexStatus>(
                LogLevel.Debug,
                default,
                "Updating the DICOM instance index status with '{DicomInstanceIdentifier}' to '{Status}'.");

        private static readonly Action<ILogger, DateTimeOffset, int, int, Exception> LogRetrieveDeletedInstancesAsyncDelegate =
            LoggerMessage.Define<DateTimeOffset, int, int>(
                LogLevel.Debug,
                default,
                "Retrieving deleted instances for cleanup at: {cleanupAfter}; batchSize: {batchSize}; maxRetries: {maxRetries}");

        private static readonly Action<ILogger, VersionedDicomInstanceIdentifier, Exception> LogDeleteDeletedInstanceAsyncDelegate =
            LoggerMessage.Define<VersionedDicomInstanceIdentifier>(
                LogLevel.Debug,
                default,
                "Removing deleted instance '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, VersionedDicomInstanceIdentifier, DateTimeOffset, Exception> LogIncrementDeletedInstanceRetryAsyncDelegate =
            LoggerMessage.Define<VersionedDicomInstanceIdentifier, DateTimeOffset>(
                LogLevel.Debug,
                default,
                "Incrementing the retry count of deleted instances '{DicomInstanceIdentifier}' and setting next cleanup time to '{cleanupAfter}'.");

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

        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly ILogger _logger;

        public LoggingDicomIndexDataStore(IDicomIndexDataStore dicomIndexDataStore, ILogger<LoggingDicomIndexDataStore> logger)
        {
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomIndexDataStore = dicomIndexDataStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<long> CreateInstanceIndexAsync(DicomDataset dicomDataset, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            LogCreateInstanceIndexDelegate(_logger, dicomDataset.ToDicomInstanceIdentifier(), null);

            try
            {
                long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dicomDataset, cancellationToken);

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
        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset deletedDate, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            LogDeleteInstanceIndexDelegate(_logger, studyInstanceUid, seriesInstanceUid, sopInstanceUid, deletedDate, cleanupAfter, null);

            try
            {
                await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, deletedDate, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset deletedDate, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            LogDeleteSeriesIndexDelegate(_logger, studyInstanceUid, seriesInstanceUid, deletedDate, cleanupAfter, null);

            try
            {
                await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, deletedDate, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset deletedDate, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            LogDeleteStudyInstanceIndexDelegate(_logger, studyInstanceUid, deletedDate, cleanupAfter, null);

            try
            {
                await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, deletedDate, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateInstanceIndexStatusAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, DicomIndexStatus status, CancellationToken cancellationToken)
        {
            LogUpdateInstanceIndexStatusDelegate(_logger, dicomInstanceIdentifier, status, null);

            try
            {
                await _dicomIndexDataStore.UpdateInstanceIndexStatusAsync(dicomInstanceIdentifier, status, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<VersionedDicomInstanceIdentifier>> RetrieveDeletedInstancesAsync(DateTimeOffset cleanupAfter, int batchSize, int maxRetries, CancellationToken cancellationToken)
        {
            LogRetrieveDeletedInstancesAsyncDelegate(_logger, cleanupAfter, batchSize, maxRetries, null);

            try
            {
                IEnumerable<VersionedDicomInstanceIdentifier> deletedInstances = await _dicomIndexDataStore.RetrieveDeletedInstancesAsync(cleanupAfter, batchSize, maxRetries, cancellationToken);

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
        public async Task DeleteDeletedInstanceAsync(VersionedDicomInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            LogDeleteDeletedInstanceAsyncDelegate(_logger, versionedInstanceIdentifier, null);

            try
            {
                await _dicomIndexDataStore.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task IncrementDeletedInstanceRetryAsync(VersionedDicomInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken)
        {
            LogIncrementDeletedInstanceRetryAsyncDelegate(_logger, versionedInstanceIdentifier, cleanupAfter, null);

            try
            {
                await _dicomIndexDataStore.IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier, cleanupAfter, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }
    }
}
