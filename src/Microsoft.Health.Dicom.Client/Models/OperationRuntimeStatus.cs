// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Specifies the status of a DICOM operation.
    /// </summary>
    public enum OperationRuntimeStatus
    {
        /// <summary>
        /// Specifies a state that is missing or unrecognized.
        /// </summary>
        Unknown,

        /// <summary>
        /// Specifies a state where execution is pending.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Specifies a state where the operation is executing and has not yet finished.
        /// </summary>
        Running,

        /// <summary>
        /// Specifies a state where the operation has finished successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Specifies a state where the operation has stopped prematurely after encountering one or more errors.
        /// </summary>
        Failed,

        /// <summary>
        /// Specifies a state where the operation that has stopped prematurely after being told to do so by the user.
        /// </summary>
        Canceled,
    }
}
