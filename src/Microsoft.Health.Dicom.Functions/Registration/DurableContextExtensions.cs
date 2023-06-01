// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Functions.Registration;

internal static class DurableContextExtensions
{
    /// <summary>
    /// Returns an instance of UpdateMeter that is replay safe, ensuring the meter emits metric only when the orchestrator
    /// is not replaying that line of code.
    /// </summary>
    /// <param name="context">The context object.</param>
    /// <param name="updateMeter">An instance of UpdateMeter.</param>
    /// <returns>An instance of a replay safe UpdateMeter.</returns>
    public static ReplaySafeUpdateMeter CreateReplaySafeMeter(this IDurableOrchestrationContext context, UpdateMeter updateMeter)
    {
        return new ReplaySafeUpdateMeter(context, updateMeter);
    }
}

