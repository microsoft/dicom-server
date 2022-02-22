// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    /// <summary>
    /// Represents a request to start a particular orchestration.
    /// </summary>
    /// <typeparam name="T">The type of the orchestration input.</typeparam>
    public sealed class OrchestrationRequest<T>
    {
        /// <summary>
        /// Gets or sets the orchestration function name.
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Gets or sets the optional instance ID.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the optional input to the orchestration.
        /// </summary>
        public T Input { get; set; }
    }
}
