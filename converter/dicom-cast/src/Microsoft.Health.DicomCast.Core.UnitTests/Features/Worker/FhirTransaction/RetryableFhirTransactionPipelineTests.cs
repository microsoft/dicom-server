// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly.Timeout;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker
{
    public class RetryableFhirTransactionPipelineTests
    {
        private TimeSpan _defaultRetryTime = new TimeSpan(0, 0, 30);
        private const int DefaultRetryCount = 3; // This value calculated based on defaultRetryTime

        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IFhirTransactionPipeline _fhirTransactionPipeline = Substitute.For<IFhirTransactionPipeline>();
        private RetryableFhirTransactionPipeline _retryableFhirTransactionPipeline;
        private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();

        public RetryableFhirTransactionPipelineTests()
        {
            RetryConfiguration config = new RetryConfiguration();
            config.TotalRetryDuration = new TimeSpan(0, 0, 30);
            _retryableFhirTransactionPipeline = new RetryableFhirTransactionPipeline(
                _fhirTransactionPipeline,
                _exceptionStore,
                Options.Create(config));
        }

        [Fact]
        public async Task GivenRetryableException_WhenProcessed_ThenItShouldRetry()
        {
            await ExecuteAndValidateRetryThenThrow(new RetryableException(), DefaultRetryCount + 1);
        }

        [Fact]
        public async Task GivenNotConflictException_WhenProcessed_ThenItShouldNotRetry()
        {
            await ExecuteAndValidate(new Exception(), 1);
        }

        [Fact]
        public async Task GivenHttpRequestExceptionException_ProcessAsync_ShouldRetryRetryableException()
        {
            await ExecuteAndValidateRetryThenThrow(new HttpRequestException(), DefaultRetryCount + 1);
        }

        [Fact]
        public async Task GivenTaskCancelledExceptionException_ProcessAsync_ShouldRetryRetryableException()
        {
            await ExecuteAndValidateRetryThenThrow(new TaskCanceledException(), DefaultRetryCount + 1);
        }

        private async Task ExecuteAndValidate(Exception ex, int expectedNumberOfCalls)
        {
            ChangeFeedEntry changeFeedEntry = ChangeFeedGenerator.Generate();

            _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, DefaultCancellationToken).Throws(ex);

            await Assert.ThrowsAsync(ex.GetType(), () => _retryableFhirTransactionPipeline.ProcessAsync(changeFeedEntry, DefaultCancellationToken));

            await _fhirTransactionPipeline.Received(expectedNumberOfCalls).ProcessAsync(changeFeedEntry, DefaultCancellationToken);
        }

        private async Task ExecuteAndValidateRetryThenThrow(Exception ex, int expectedNumberOfCalls)
        {
            ChangeFeedEntry changeFeedEntry = ChangeFeedGenerator.Generate();

            _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, DefaultCancellationToken).Throws(ex);

            await Assert.ThrowsAsync<TimeoutRejectedException>(() => _retryableFhirTransactionPipeline.ProcessAsync(changeFeedEntry, DefaultCancellationToken));

            await _fhirTransactionPipeline.Received(expectedNumberOfCalls).ProcessAsync(changeFeedEntry, DefaultCancellationToken);
        }
    }
}
