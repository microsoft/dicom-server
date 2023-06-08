// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Registration;

internal static class DurableContextExtensions
{
    /// <summary>
    /// Returns an instance of Counter that is replay safe, ensuring the meter emits metric only when the orchestrator
    /// is not replaying that line of code.
    /// </summary>
    /// <param name="context">The context object.</param>
    /// <param name="counter">A metric counter.</param>
    /// <returns>An instance of a replay safe Counter.</returns>
    public static ReplaySafeCounter<int> CreateReplaySafeCounter(this IDurableOrchestrationContext context, Counter<int> counter)
    {
        return new ReplaySafeCounter<int>(context, counter);
    }
}

