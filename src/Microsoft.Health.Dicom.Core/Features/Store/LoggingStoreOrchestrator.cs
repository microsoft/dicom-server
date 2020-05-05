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
    /// Provides logging for <see cref="IStoreOrchestrator"/>.
    /// </summary>
    public class LoggingStoreOrchestrator : IStoreOrchestrator
    {
        private static readonly Action<ILogger, string, Exception> LogPersistingDicomInstanceEntryDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Storing a DICOM instance: '{DicomInstance}'.");

        private static readonly Action<ILogger, string, Exception> LogSuccessfullyPersistedDicomInstanceEntryDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Successfully stored the DICOM instance: '{DicomInstance}'.");

        private static readonly Action<ILogger, string, Exception> LogFailedToPersistDicomInstanceEntryDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                default,
                "Failed to store the DICOM instance: '{DicomInstance}'.");

        private readonly IStoreOrchestrator _storeOrchestrator;
        private readonly ILogger _logger;

        public LoggingStoreOrchestrator(
            IStoreOrchestrator storeOrchestrator,
            ILogger<LoggingStoreOrchestrator> logger)
        {
            EnsureArg.IsNotNull(storeOrchestrator, nameof(storeOrchestrator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _storeOrchestrator = storeOrchestrator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StoreDicomInstanceEntryAsync(IInstanceEntry instanceEntry, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instanceEntry, nameof(instanceEntry));

            string dicomInstanceIdentifier = (await instanceEntry.GetDicomDatasetAsync(cancellationToken))
                .ToDicomInstanceIdentifier()
                .ToString();

            LogPersistingDicomInstanceEntryDelegate(_logger, dicomInstanceIdentifier, null);

            try
            {
                await _storeOrchestrator.StoreDicomInstanceEntryAsync(instanceEntry, cancellationToken);

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
