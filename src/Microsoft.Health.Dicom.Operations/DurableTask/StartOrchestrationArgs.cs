// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask
{
    // Note: There is a non-generic version of this class provided by the Durable Task Framework

    /// <summary>
    /// Represents the arguments for a new orchestration instance.
    /// </summary>
    /// <typeparam name="T">The type of the orchestration input.</typeparam>
    public sealed class StartOrchestrationArgs<T>
    {
        /// <summary>
        /// Gets or sets the orchestration function name.
        /// </summary>
        public string FunctionName { get; init; }

        /// <summary>
        /// Gets or sets the optional instance ID.
        /// </summary>
        public string InstanceId { get; init; }

        /// <summary>
        /// Gets or sets the optional input to the orchestration.
        /// </summary>
        public T Input { get; init; }
    }
}
