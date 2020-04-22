// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public class LoggingDicomFileStore : IDicomFileStore
    {
        private static readonly Action<ILogger, string, bool, Exception> LogAddFileDelegate =
            LoggerMessage.Define<string, bool>(
                LogLevel.Debug,
                default,
                "Adding DICOM instance file with '{DicomInstanceIdentifier}' using overwrite mode '{OverwriteMode}'.");

        private static readonly Action<ILogger, string, Exception> LogDeleteFileDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                default,
                "Deleting DICOM instance file with '{DicomInstanceIdentifier}'.");

        private static readonly Action<ILogger, string, Exception> LogGetFileDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                default,
                "Getting the DICOM instance file with '{DicomInstanceIdentifier}'.");

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

        private readonly IDicomFileStore _dicomFileStore;
        private readonly ILogger _logger;

        public LoggingDicomFileStore(IDicomFileStore dicomFileStore, ILogger<LoggingDicomFileStore> logger)
        {
            EnsureArg.IsNotNull(dicomFileStore, nameof(dicomFileStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomFileStore = dicomFileStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Uri> AddFileAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, Stream stream, bool overwriteIfExists = false, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));

            LogAddFileDelegate(_logger, dicomInstanceIdentifier.ToString(), overwriteIfExists, null);

            try
            {
                Uri uri = await _dicomFileStore.AddFileAsync(dicomInstanceIdentifier, stream, overwriteIfExists, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return uri;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteFileIfExistsAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));

            LogDeleteFileDelegate(_logger, dicomInstanceIdentifier.ToString(), null);

            try
            {
                await _dicomFileStore.DeleteFileIfExistsAsync(dicomInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Stream> GetFileAsync(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));

            LogGetFileDelegate(_logger, dicomInstanceIdentifier.ToString(), null);

            try
            {
                Stream stream = await _dicomFileStore.GetFileAsync(dicomInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);

                return stream;
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }
    }
}
