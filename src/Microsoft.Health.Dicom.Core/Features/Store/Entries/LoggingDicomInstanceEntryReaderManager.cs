// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Provides logging for <see cref="IDicomInstanceEntryReaderManager"/>.
    /// </summary>
    public class LoggingDicomInstanceEntryReaderManager : IDicomInstanceEntryReaderManager
    {
        private static readonly Action<ILogger, string, Exception> LogFindingReaderDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Trace,
                default,
                $"Looking for an {nameof(IDicomInstanceEntryReader)} that can process content type '{{ContentType}}'.");

        private static readonly Action<ILogger, Exception> LogNoReaderFoundDelegate =
            LoggerMessage.Define(
                LogLevel.Debug,
                default,
                "No reader was found.");

        private static readonly Action<ILogger, string, Exception> LogReaderFoundDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                default,
                "Found the reader with '{ReaderType}'.");

        private readonly IDicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager;
        private readonly ILogger _logger;

        public LoggingDicomInstanceEntryReaderManager(
            IDicomInstanceEntryReaderManager dicomInstanceEntryReaderManager,
            ILogger<DicomInstanceEntryReaderManager> logger)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReaderManager, nameof(dicomInstanceEntryReaderManager));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomInstanceEntryReaderManager = dicomInstanceEntryReaderManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public IDicomInstanceEntryReader FindReader(string contentType)
        {
            LogFindingReaderDelegate(_logger, contentType, null);

            IDicomInstanceEntryReader dicomInstanceEntryReader = _dicomInstanceEntryReaderManager.FindReader(contentType);

            if (dicomInstanceEntryReader == null)
            {
                LogNoReaderFoundDelegate(_logger, null);
            }
            else
            {
                LogReaderFoundDelegate(_logger, dicomInstanceEntryReader.GetType().Name, null);
            }

            return dicomInstanceEntryReader;
        }
    }
}
