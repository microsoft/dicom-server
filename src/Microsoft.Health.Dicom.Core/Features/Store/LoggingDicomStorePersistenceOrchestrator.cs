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
        private static readonly Action<ILogger, string, Exception> LogStoringUploadedDicomInstanceDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Storing an uploaded DICOM instance: '{UploadedDicomInstance}'.");

        private static readonly Action<ILogger, string, Exception> LogSuccessfullyStoredUploadedDicomInstanceDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Successfully stored the uploaded DICOM instance: '{UploadedDicomInstance}'.");

        private static readonly Action<ILogger, string, Exception> LogFailedToStoreUploadedDicomInstanceDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                default,
                "Failed to store the uploaded DICOM instance: '{UploadedDicomInstance}'.");

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
        public async Task PersistDicomInstanceEntryAsync(IDicomInstanceEntry uploadedDicomInstance, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(uploadedDicomInstance, nameof(uploadedDicomInstance));

            string dicomInstanceIdentifier = (await uploadedDicomInstance.GetDicomDatasetAsync(cancellationToken))
                .ToDicomInstanceIdentifier()
                .ToString();

            LogStoringUploadedDicomInstanceDelegate(_logger, dicomInstanceIdentifier, null);

            try
            {
                await _dicomStorePersistenceOrchestrator.PersistDicomInstanceEntryAsync(uploadedDicomInstance, cancellationToken);

                LogSuccessfullyStoredUploadedDicomInstanceDelegate(_logger, dicomInstanceIdentifier, null);
            }
            catch (Exception ex)
            {
                LogFailedToStoreUploadedDicomInstanceDelegate(_logger, dicomInstanceIdentifier, ex);

                throw;
            }
        }
    }
}
