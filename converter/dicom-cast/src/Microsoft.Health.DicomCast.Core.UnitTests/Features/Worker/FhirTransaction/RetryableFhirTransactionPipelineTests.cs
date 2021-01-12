// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker
{
    public class RetryableFhirTransactionPipelineTests
    {
        private const int DefaultRetryCount = 3;

        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IFhirTransactionPipeline _fhirTransactionPipeline = Substitute.For<IFhirTransactionPipeline>();
        private RetryableFhirTransactionPipeline _retryableFhirTransactionPipeline;

        public RetryableFhirTransactionPipelineTests()
        {
            _retryableFhirTransactionPipeline = new RetryableFhirTransactionPipeline(
                _fhirTransactionPipeline);
        }

        [Fact]
        public async Task GivenResourceConflictException_WhenProcessed_ThenItShouldRetry()
        {
            await ExecuteAndValidate(new ResourceConflictException(), DefaultRetryCount + 1);
        }

        [Fact]
        public async Task GivenServerTooBusyException_WhenProcessed_ThenItShouldRetry()
        {
            await ExecuteAndValidate(new ServerTooBusyException(), DefaultRetryCount + 1);
        }

        [Fact]
        public async Task GivenNotConflictException_WhenProcessed_ThenItShouldNotRetry()
        {
            await ExecuteAndValidate(new Exception(), 1);
        }

        private async Task ExecuteAndValidate(Exception ex, int expectedNumberOfCalls)
        {
            ChangeFeedEntry changeFeedEntry = ChangeFeedGenerator.Generate();

            _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, DefaultCancellationToken).Throws(ex);

            await Assert.ThrowsAsync(ex.GetType(), () => _retryableFhirTransactionPipeline.ProcessAsync(changeFeedEntry, DefaultCancellationToken));

            await _fhirTransactionPipeline.Received(expectedNumberOfCalls).ProcessAsync(changeFeedEntry, DefaultCancellationToken);
        }
    }
}
