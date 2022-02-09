// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Representing the Procedure Step State.
    /// </summary>
    public enum ProcedureStepState
    {
        /// <summary>
        /// Empty Procedure Step State.
        /// </summary>
        [Display(Name = @"")]
        None,

        /// <summary>
        /// The UPS is scheduled to be performed.
        /// </summary>
        [Display(Name = @"SCHEDULED")]
        Scheduled,

        /// <summary>
        /// The UPS has been claimed and a Locking UID has been set. Performance of the UPS has likely started.
        /// </summary>
        [Display(Name = @"IN PROGRESS")]
        InProgress,

        /// <summary>
        /// The UPS has been completed.
        /// </summary>
        [Display(Name = @"COMPLETED")]
        Completed,

        /// <summary>
        /// The UPS has been permanently stopped before or during performance of the step.
        /// This may be due to voluntary or involuntary action by a human or machine.
        /// Any further UPS-driven work required to complete the scheduled task must be performed by scheduling another (different) UPS.
        /// </summary>
        [Display(Name = @"CANCELED")]
        Canceled
    }
}
