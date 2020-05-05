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
    /// Provides logging for <see cref="IInstanceEntryReaderManager"/>.
    /// </summary>
    public class LoggingInstanceEntryReaderManager : IInstanceEntryReaderManager
    {
        private static readonly Action<ILogger, string, Exception> LogFindingReaderDelegate =
            LoggerMessage.Define<string>(
                LogLevel.Trace,
                default,
                $"Looking for an {nameof(IInstanceEntryReader)} that can process content type '{{ContentType}}'.");

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

        private readonly IInstanceEntryReaderManager _instanceEntryReaderManager;
        private readonly ILogger _logger;

        public LoggingInstanceEntryReaderManager(
            IInstanceEntryReaderManager instanceEntryReaderManager,
            ILogger<InstanceEntryReaderManager> logger)
        {
            EnsureArg.IsNotNull(instanceEntryReaderManager, nameof(instanceEntryReaderManager));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _instanceEntryReaderManager = instanceEntryReaderManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public IInstanceEntryReader FindReader(string contentType)
        {
            LogFindingReaderDelegate(_logger, contentType, null);

            IInstanceEntryReader instanceEntryReader = _instanceEntryReaderManager.FindReader(contentType);

            if (instanceEntryReader == null)
            {
                LogNoReaderFoundDelegate(_logger, null);
            }
            else
            {
                LogReaderFoundDelegate(_logger, instanceEntryReader.GetType().Name, null);
            }

            return instanceEntryReader;
        }
    }
}
