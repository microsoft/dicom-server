// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class LoggingFhirTransactionPipelineStep : IFhirTransactionPipelineStep
    {
        private static readonly Func<ILogger, string, IDisposable> LogPreparingRequestDelegate =
            LoggerMessage.DefineScope<string>($"Executing {nameof(PrepareRequestAsync)} of {{PipelineStep}}.");

        private static readonly Func<ILogger, string, IDisposable> LogProcessingResponseDelegate =
            LoggerMessage.DefineScope<string>($"Executing {nameof(ProcessResponse)} of {{PipelineStep}}.");

        private static readonly Action<ILogger, Exception> LogExceptionDelegate =
            LoggerMessage.Define(
                LogLevel.Error,
                default,
                "Encountered an exception while processing.");

        private readonly IFhirTransactionPipelineStep _fhirTransactionPipelineStep;
        private readonly ILogger _logger;
        private readonly string _pipelineStepName;

        public LoggingFhirTransactionPipelineStep(
            IFhirTransactionPipelineStep fhirTransactionPipelineStep,
            ILogger<LoggingFhirTransactionPipelineStep> logger)
        {
            EnsureArg.IsNotNull(fhirTransactionPipelineStep, nameof(fhirTransactionPipelineStep));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirTransactionPipelineStep = fhirTransactionPipelineStep;
            _logger = logger;
            _pipelineStepName = fhirTransactionPipelineStep.GetType().Name;
        }

        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            using (LogPreparingRequestDelegate(_logger, _pipelineStepName))
            {
                try
                {
                    await _fhirTransactionPipelineStep.PrepareRequestAsync(context, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancel requested.
                    throw;
                }
                catch (Exception ex)
                {
                    LogExceptionDelegate(_logger, ex);
                    throw;
                }
            }
        }

        public void ProcessResponse(FhirTransactionContext context)
        {
            using (LogProcessingResponseDelegate(_logger, _pipelineStepName))
            {
                try
                {
                    _fhirTransactionPipelineStep.ProcessResponse(context);
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
