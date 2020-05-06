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
    /// Provides logging for <see cref="IDicomInstanceEntryReader"/>.
    /// </summary>
    public class LoggingDicomInstanceEntryReader : IDicomInstanceEntryReader
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

        private readonly IDicomInstanceEntryReader _dicomInstanceEntryReader;
        private readonly ILogger _logger;

        private readonly string _readerType;

        public LoggingDicomInstanceEntryReader(
            IDicomInstanceEntryReader dicomInstanceEntryReader,
            ILogger<LoggingDicomInstanceEntryReader> logger)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReader, nameof(dicomInstanceEntryReader));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomInstanceEntryReader = dicomInstanceEntryReader;
            _logger = logger;

            _readerType = _dicomInstanceEntryReader.GetType().Name;
        }

        /// <inheritdoc />
        public bool CanRead(string contentType)
        {
            LogCanReadDelegate(_logger, _readerType, contentType, null);

            return _dicomInstanceEntryReader.CanRead(contentType);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IDicomInstanceEntry>> ReadAsync(string contentType, Stream stream, CancellationToken cancellationToken)
        {
            LogReadingDelegate(_logger, _readerType, null);

            try
            {
                IReadOnlyList<IDicomInstanceEntry> dicomInstanceEntries = await _dicomInstanceEntryReader.ReadAsync(contentType, stream);

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
