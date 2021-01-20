// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
        private const int MaxRetryCount = 3;

        // private static readonly TimeSpan SleepDuration = TimeSpan.FromMilliseconds(100);

        private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
        private readonly IExceptionStore _exceptionStore;
        private readonly IAsyncPolicy _retryPolicy;

        public RetryableFhirTransactionPipeline(IFhirTransactionPipeline fhirTransactionPipeline, IExceptionStore exceptionStore)
            : this(
                  fhirTransactionPipeline,
                  exceptionStore,
                  MaxRetryCount)
        {
        }

        internal RetryableFhirTransactionPipeline(
            IFhirTransactionPipeline fhirTransactionPipeline,
            IExceptionStore exceptionStore,
            int maxRetryCount)
        {
            EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _fhirTransactionPipeline = fhirTransactionPipeline;
            _exceptionStore = exceptionStore;
            _retryPolicy = Policy
                .Handle<RetryableException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    maxRetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        CancellationToken token = (CancellationToken)context[nameof(CancellationToken)];
                        if (exception is TaskCanceledException && token.IsCancellationRequested)
                        {
                            return Task.CompletedTask;
                        }

                        ChangeFeedEntry changeFeedEntry = (ChangeFeedEntry)context[nameof(ChangeFeedEntry)];

                        return _exceptionStore.WriteRetryableExceptionAsync(
                            changeFeedEntry,
                            retryCount,
                            exception);
                    });
        }

        /// <inheritdoc/>
        public Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
        {
            Context context = new Context();
            context[nameof(ChangeFeedEntry)] = changeFeedEntry;
            context[nameof(CancellationToken)] = cancellationToken;
            return _retryPolicy.ExecuteAsync(_ => _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken), context);
        }
    }
}
