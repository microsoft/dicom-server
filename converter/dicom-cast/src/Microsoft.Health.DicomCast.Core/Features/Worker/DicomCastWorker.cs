// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker
{
    /// <summary>
    /// The worker for DicomCast.
    /// </summary>
    public class DicomCastWorker : IDicomCastWorker
    {
        private static readonly Action<ILogger, Exception> LogWorkerStartingDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                $"{typeof(DicomCastWorker)} is starting.");

        private static readonly Action<ILogger, Exception> LogWorkerCancelRequestedDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                $"{typeof(DicomCastWorker)} is requested to be stopped.");

        private static readonly Action<ILogger, Exception> LogWorkerExitingDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                $"{typeof(DicomCastWorker)} is exiting.");

        private readonly DicomCastWorkerConfiguration _dicomCastWorkerConfiguration;
        private readonly IChangeFeedProcessor _changeFeedProcessor;
        private readonly ILogger _logger;

        public DicomCastWorker(
            IOptions<DicomCastWorkerConfiguration> dicomCastWorkerConfiguration,
            IChangeFeedProcessor changeFeedProcessor,
            ILogger<DicomCastWorker> logger)
        {
            EnsureArg.IsNotNull(dicomCastWorkerConfiguration?.Value, nameof(dicomCastWorkerConfiguration));
            EnsureArg.IsNotNull(changeFeedProcessor, nameof(changeFeedProcessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomCastWorkerConfiguration = dicomCastWorkerConfiguration.Value;
            _changeFeedProcessor = changeFeedProcessor;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            LogWorkerStartingDelegate(_logger, null);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _changeFeedProcessor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, cancellationToken);

                    await Task.Delay(_dicomCastWorkerConfiguration.PollInterval, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancel requested.
                    LogWorkerCancelRequestedDelegate(_logger, null);
                    break;
                }
            }

            LogWorkerExitingDelegate(_logger, null);
        }
    }
}
