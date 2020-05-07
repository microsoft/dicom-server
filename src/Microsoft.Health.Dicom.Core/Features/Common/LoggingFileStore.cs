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
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    public class LoggingFileStore : IFileStore
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

        private readonly IFileStore _fileStore;
        private readonly ILogger _logger;

        public LoggingFileStore(IFileStore fileStore, ILogger<LoggingFileStore> logger)
        {
            EnsureArg.IsNotNull(fileStore, nameof(fileStore));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fileStore = fileStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Uri> AddFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, Stream stream, bool overwriteIfExists = false, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            LogAddFileDelegate(_logger, versionedInstanceIdentifier.ToString(), overwriteIfExists, null);

            try
            {
                Uri uri = await _fileStore.AddFileAsync(versionedInstanceIdentifier, stream, overwriteIfExists, cancellationToken);

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
        public async Task DeleteFileIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            LogDeleteFileDelegate(_logger, versionedInstanceIdentifier.ToString(), null);

            try
            {
                await _fileStore.DeleteFileIfExistsAsync(versionedInstanceIdentifier, cancellationToken);

                LogOperationSucceededDelegate(_logger, null);
            }
            catch (Exception ex)
            {
                LogOperationFailedDelegate(_logger, ex);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Stream> GetFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            LogGetFileDelegate(_logger, versionedInstanceIdentifier.ToString(), null);

            try
            {
                Stream stream = await _fileStore.GetFileAsync(versionedInstanceIdentifier, cancellationToken);

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
