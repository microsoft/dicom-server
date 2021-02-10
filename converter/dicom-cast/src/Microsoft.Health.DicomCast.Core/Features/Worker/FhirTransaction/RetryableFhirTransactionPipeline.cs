// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Polly;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides retry functionality to <see cref="IFhirTransactionPipeline"/>.
    /// </summary>
    public class RetryableFhirTransactionPipeline : IFhirTransactionPipeline
    {
        private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
        private readonly IExceptionStore _exceptionStore;
        private readonly IAsyncPolicy _retryPolicy;

        public RetryableFhirTransactionPipeline(IFhirTransactionPipeline fhirTransactionPipeline, IExceptionStore exceptionStore, int maxRetryCount = 15)
        {
            EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _fhirTransactionPipeline = fhirTransactionPipeline;
            _exceptionStore = exceptionStore;
            _retryPolicy = Policy
                .Handle<RetryableException>()
                .WaitAndRetryAsync(
                    maxRetryCount,
                    (retryAttempt) =>
                    {
                        double waitTime = Math.Pow(2, retryAttempt);
                        return waitTime < 60 ? TimeSpan.FromSeconds(waitTime) : TimeSpan.FromSeconds(60);
                    },
                    (exception, timeSpan, retryCount, context) =>
                    {
                        ChangeFeedEntry changeFeedEntry = (ChangeFeedEntry)context[nameof(ChangeFeedEntry)];

                        return _exceptionStore.WriteRetryableExceptionAsync(
                            changeFeedEntry,
                            retryCount,
                            timeSpan.TotalSeconds,
                            exception);
                    });
        }

        /// <inheritdoc/>
        public Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
        {
            Context context = new Context();
            context[nameof(ChangeFeedEntry)] = changeFeedEntry;

            return _retryPolicy.ExecuteAsync(
                async (ctx, tkn) =>
                {
                    try
                    {
                       await _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken);
                    }
                    catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new RetryableException(ex);
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new RetryableException(ex);
                    }
                },
                context,
                cancellationToken);
        }
    }
}
