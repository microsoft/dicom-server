// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Core.Features.Store.Upload
{
    /// <summary>
    /// Provides logging for <see cref="IUploadedDicomInstanceReader"/>.
    /// </summary>
    public class LoggingUploadedDicomInstanceReader : IUploadedDicomInstanceReader
    {
        private static readonly Action<ILogger, string, string, Exception> LogCanReadDelegate =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                default,
                "Checking if {UploadedDicomInstanceReaderType} can read {ContentType}.");

        private static readonly Action<ILogger, string, Exception> LogReadingDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Reading uploaded DICOM instances using '{UploadedDicomInstanceReaderType}'.");

        private static readonly Action<ILogger, int, Exception> LogSuccessfullyReadDelegate =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                default,
                "Successfully read {Number} instances(s).");

        private static readonly Action<ILogger, Exception> LogFailedToReadDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "Failed to read uploaded DICOM instance(s).");

        private readonly IUploadedDicomInstanceReader _uploadedDicomInstanceReader;
        private readonly ILogger _logger;

        private readonly string _readerType;

        public LoggingUploadedDicomInstanceReader(
            IUploadedDicomInstanceReader uploadedDicomInstanceReader,
            ILogger<LoggingUploadedDicomInstanceReader> logger)
        {
            EnsureArg.IsNotNull(uploadedDicomInstanceReader, nameof(uploadedDicomInstanceReader));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _uploadedDicomInstanceReader = uploadedDicomInstanceReader;
            _logger = logger;

            _readerType = _uploadedDicomInstanceReader.GetType().Name;
        }

        /// <inheritdoc />
        public bool CanRead(string contentType)
        {
            LogCanReadDelegate(_logger, _readerType, contentType, null);

            return _uploadedDicomInstanceReader.CanRead(contentType);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<IUploadedDicomInstance>> ReadAsync(string contentType, Stream stream, CancellationToken cancellationToken)
        {
            LogReadingDelegate(_logger, _readerType, null);

            try
            {
                IReadOnlyCollection<IUploadedDicomInstance> uploadedDicomInstances = await _uploadedDicomInstanceReader.ReadAsync(contentType, stream);

                LogSuccessfullyReadDelegate(_logger, uploadedDicomInstances?.Count ?? 0, null);

                return uploadedDicomInstances;
            }
            catch (Exception ex)
            {
                LogFailedToReadDelegate(_logger, ex);

                throw;
            }
        }
    }
}
