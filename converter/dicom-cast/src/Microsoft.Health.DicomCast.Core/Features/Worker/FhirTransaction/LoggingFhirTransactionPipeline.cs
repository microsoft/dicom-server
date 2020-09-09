// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides logging for <see cref="IFhirTransactionPipeline"/>.
    /// </summary>
    public class LoggingFhirTransactionPipeline : IFhirTransactionPipeline
    {
        private static readonly Func<ILogger, long, IDisposable> LogProcessingDelegate =
            LoggerMessage.DefineScope<long>("Processing change feed with '{ChangeFeedEntrySequence}'.");

        private static readonly Action<ILogger, Exception> LogSuccessfullyProcessedDelegate =
            LoggerMessage.Define(
                LogLevel.Information,
                default,
                "Successfully processed the change feed entry.");

        private static readonly Action<ILogger, Exception> LogExceptionDelegate =
            LoggerMessage.Define(
                LogLevel.Error,
                default,
                "Encountered an exception while processing the change feed entry.");

        private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
        private readonly ILogger _logger;

        public LoggingFhirTransactionPipeline(
            IFhirTransactionPipeline fhirTransactionPipeline,
            ILogger<LoggingFhirTransactionPipeline> logger)
        {
            EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirTransactionPipeline = fhirTransactionPipeline;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(changeFeedEntry, nameof(changeFeedEntry));

            using (LogProcessingDelegate(_logger, changeFeedEntry.Sequence))
            {
                try
                {
                    await _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken);

                    LogSuccessfullyProcessedDelegate(_logger, null);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancel requested
                    throw;
                }
                catch (Exception ex)
                {
                    LogExceptionDelegate(_logger, ex);
                    throw;
                }
            }
        }
    }
}
