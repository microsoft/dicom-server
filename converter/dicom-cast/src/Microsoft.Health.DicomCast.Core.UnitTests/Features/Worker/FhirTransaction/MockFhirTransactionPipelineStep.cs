// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class MockFhirTransactionPipelineStep : IFhirTransactionPipelineStep
    {
        public Action<FhirTransactionContext, CancellationToken> OnPrepareRequestAsyncCalled { get; set; }

        public Action<FhirTransactionContext> OnProcessResponseCalled { get; set; }

        public Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            OnPrepareRequestAsyncCalled?.Invoke(context, cancellationToken);

            return Task.CompletedTask;
        }

        public void ProcessResponse(FhirTransactionContext context)
        {
            OnProcessResponseCalled?.Invoke(context);
        }
    }
}
