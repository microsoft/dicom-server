// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

public abstract class FhirTransactionPipelineStepBase : IFhirTransactionPipelineStep
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

    private readonly ILogger _logger;
    private readonly string _pipelineStepName;

    protected FhirTransactionPipelineStepBase(
        ILogger<FhirTransactionPipelineStepBase> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));

        _logger = logger;
        _pipelineStepName = GetType().Name;
    }

    public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken = default)
    {
        using (LogPreparingRequestDelegate(_logger, _pipelineStepName))
        {
            try
            {
                await PrepareRequestImplementationAsync(context, cancellationToken);
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
                ProcessResponseImplementation(context);
            }
            catch (Exception ex)
            {
                LogExceptionDelegate(_logger, ex);
                throw;
            }
        }
    }

    protected abstract Task PrepareRequestImplementationAsync(FhirTransactionContext context, CancellationToken cancellationToken = default);

    protected abstract void ProcessResponseImplementation(FhirTransactionContext context);
}
