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
                .WaitAndRetryAsync(
                    maxRetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    async (exception, timeSpan, retryCount, context) =>
                    {
                        if (exception is RetryableException)
                        {
                            RetryableException ex = (RetryableException)exception;
                            ChangeFeedEntry changeFeedEntry = ex.ChangeFeedEntry;
                            string studyUid = changeFeedEntry.StudyInstanceUid;
                            string seriesUid = changeFeedEntry.SeriesInstanceUid;
                            string instanceUid = changeFeedEntry.SopInstanceUid;
                            long changeFeedSequence = changeFeedEntry.Sequence;

                            // Todo need to overload the storeException method to store the retry number as well and figure out the cancellation token 

                            await _exceptionStore.StoreException(
                                studyUid,
                                seriesUid,
                                instanceUid,
                                changeFeedSequence,
                                ex,
                                ErrorType.TransientRetry);
                        }
                    });
        }

        /// <inheritdoc/>
        public Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
            => _retryPolicy.ExecuteAsync(() => _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken));
    }
}
