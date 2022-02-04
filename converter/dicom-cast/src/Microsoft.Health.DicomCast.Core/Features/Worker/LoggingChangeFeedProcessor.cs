// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.DicomCast.Core.Features.Worker
{
    /// <summary>
    /// Provides logging for <see cref="IChangeFeedProcessor"/>.
    /// </summary>
    public class LoggingChangeFeedProcessor : IChangeFeedProcessor
    {
        private static readonly Func<ILogger, IDisposable> LogProcessingDelegate =
            LoggerMessage.DefineScope("Processing change feed.");

        private readonly IChangeFeedProcessor _changeFeedProcessor;
        private readonly ILogger _logger;

        public LoggingChangeFeedProcessor(
            IChangeFeedProcessor changeFeedProcessor,
            ILogger<LoggingChangeFeedProcessor> logger)
        {
            EnsureArg.IsNotNull(changeFeedProcessor, nameof(changeFeedProcessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _changeFeedProcessor = changeFeedProcessor;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task ProcessAsync(TimeSpan pollIntervalDuringCatchup, CancellationToken cancellationToken)
        {
            using (LogProcessingDelegate(_logger))
            {
                await _changeFeedProcessor.ProcessAsync(pollIntervalDuringCatchup, cancellationToken);
            }
        }
    }
}
