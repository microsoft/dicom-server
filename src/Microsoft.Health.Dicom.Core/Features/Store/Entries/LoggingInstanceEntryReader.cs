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

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Provides logging for <see cref="IInstanceEntryReader"/>.
    /// </summary>
    public class LoggingInstanceEntryReader : IInstanceEntryReader
    {
        private static readonly Action<ILogger, string, string, Exception> LogCanReadDelegate =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                default,
                "Checking if {DicomInstanceReaderEntryType} can read {ContentType}.");

        private static readonly Action<ILogger, string, Exception> LogReadingDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                default,
                "Reading DICOM instance entries using '{DicomInstanceEntryReaderType}'.");

        private static readonly Action<ILogger, int, Exception> LogSuccessfullyReadDelegate =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                default,
                "Successfully read {Number} instance entries.");

        private static readonly Action<ILogger, Exception> LogFailedToReadDelegate =
            LoggerMessage.Define(
                LogLevel.Warning,
                default,
                "Failed to read DICOM instance entries.");

        private readonly IInstanceEntryReader _instanceEntryReader;
        private readonly ILogger _logger;

        private readonly string _readerType;

        public LoggingInstanceEntryReader(
            IInstanceEntryReader instanceEntryReader,
            ILogger<LoggingInstanceEntryReader> logger)
        {
            EnsureArg.IsNotNull(instanceEntryReader, nameof(instanceEntryReader));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _instanceEntryReader = instanceEntryReader;
            _logger = logger;

            _readerType = _instanceEntryReader.GetType().Name;
        }

        /// <inheritdoc />
        public bool CanRead(string contentType)
        {
            LogCanReadDelegate(_logger, _readerType, contentType, null);

            return _instanceEntryReader.CanRead(contentType);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IInstanceEntry>> ReadAsync(string contentType, Stream stream, CancellationToken cancellationToken)
        {
            LogReadingDelegate(_logger, _readerType, null);

            try
            {
                IReadOnlyList<IInstanceEntry> dicomInstanceEntries = await _instanceEntryReader.ReadAsync(contentType, stream);

                LogSuccessfullyReadDelegate(_logger, dicomInstanceEntries?.Count ?? 0, null);

                return dicomInstanceEntries;
            }
            catch (Exception ex)
            {
                LogFailedToReadDelegate(_logger, ex);

                throw;
            }
        }
    }
}
