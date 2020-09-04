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
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Polly;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides retry functionality to <see cref="IFhirTransactionPipeline"/>.
    /// </summary>
    public class RetryableFhirTransactionPipeline : IFhirTransactionPipeline
    {
        private const int MaxRetryCount = 3;
        private static readonly TimeSpan SleepDuration = TimeSpan.FromMilliseconds(100);

        private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
        private readonly IAsyncPolicy _retryPolicy;

        public RetryableFhirTransactionPipeline(IFhirTransactionPipeline fhirTransactionPipeline)
            : this(
                  fhirTransactionPipeline,
                  MaxRetryCount,
                  retryCount => retryCount == 0 ? TimeSpan.Zero : SleepDuration)
        {
        }

        internal RetryableFhirTransactionPipeline(
            IFhirTransactionPipeline fhirTransactionPipeline,
            int retryCount,
            Func<int, TimeSpan> sleepDurationProvider)
        {
            EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));

            _fhirTransactionPipeline = fhirTransactionPipeline;
            _retryPolicy = Policy
                .Handle<ResourceConflictException>()
                .Or<ServerTooBusyException>()
                .WaitAndRetryAsync(retryCount, sleepDurationProvider);
        }

        /// <inheritdoc/>
        public Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
            => _retryPolicy.ExecuteAsync(() => _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken));
    }
}
