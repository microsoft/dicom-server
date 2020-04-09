// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides logging for <see cref="IDicomStorePersistenceOrchestrator"/>.
    /// </summary>
    public class LoggingDicomStorePersistenceOrchestrator : IDicomStorePersistenceOrchestrator
    {
        private static readonly Action<ILogger, string, Exception> LogPersistingDicomInstanceEntryDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Persisting a DICOM instance entry: '{DicomInstanceEntry}'.");

        private static readonly Action<ILogger, string, Exception> LogSuccessfullyPersistedDicomInstanceEntryDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Successfully persisted the DICOM instance entry: '{DicomInstanceEntry}'.");

        private static readonly Action<ILogger, string, Exception> LogFailedToPersistDicomInstanceEntryDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                default,
                "Failed to persist the DICOM instance entry: '{DicomInstanceEntry}'.");

        private readonly IDicomStorePersistenceOrchestrator _dicomStorePersistenceOrchestrator;
        private readonly ILogger _logger;

        public LoggingDicomStorePersistenceOrchestrator(
            IDicomStorePersistenceOrchestrator dicomStorePersistenceOrchestrator,
            ILogger<LoggingDicomStorePersistenceOrchestrator> logger)
        {
            EnsureArg.IsNotNull(dicomStorePersistenceOrchestrator, nameof(dicomStorePersistenceOrchestrator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomStorePersistenceOrchestrator = dicomStorePersistenceOrchestrator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task PersistDicomInstanceEntryAsync(IDicomInstanceEntry dicomInstanceEntry, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomInstanceEntry, nameof(dicomInstanceEntry));

            string dicomInstanceIdentifier = (await dicomInstanceEntry.GetDicomDatasetAsync(cancellationToken))
                .ToDicomInstanceIdentifier()
                .ToString();

            LogPersistingDicomInstanceEntryDelegate(_logger, dicomInstanceIdentifier, null);

            try
            {
                await _dicomStorePersistenceOrchestrator.PersistDicomInstanceEntryAsync(dicomInstanceEntry, cancellationToken);

                LogSuccessfullyPersistedDicomInstanceEntryDelegate(_logger, dicomInstanceIdentifier, null);
            }
            catch (Exception ex)
            {
                LogFailedToPersistDicomInstanceEntryDelegate(_logger, dicomInstanceIdentifier, ex);

                throw;
            }
        }
    }
}
