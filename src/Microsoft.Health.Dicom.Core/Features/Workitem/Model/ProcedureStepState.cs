// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Representing the Procedure Step State.
/// </summary>
public enum ProcedureStepState
{
    /// <summary>
    /// Empty Procedure Step State.
    /// </summary>
    [Display(Name = ProcedureStepStateConstants.None)]
    None,

    /// <summary>
    /// The UPS is scheduled to be performed.
    /// </summary>
    [Display(Name = ProcedureStepStateConstants.Scheduled)]
    Scheduled,

    /// <summary>
    /// The UPS has been claimed and a Locking UID has been set. Performance of the UPS has likely started.
    /// </summary>
    [Display(Name = ProcedureStepStateConstants.InProgress)]
    InProgress,

    /// <summary>
    /// The UPS has been completed.
    /// </summary>
    [Display(Name = ProcedureStepStateConstants.Completed)]
    Completed,

    /// <summary>
    /// The UPS has been permanently stopped before or during performance of the step.
    /// This may be due to voluntary or involuntary action by a human or machine.
    /// Any further UPS-driven work required to complete the scheduled task must be performed by scheduling another (different) UPS.
    /// </summary>
    [Display(Name = ProcedureStepStateConstants.Canceled)]
    Canceled
}

internal static class ProcedureStepStateConstants
{
    internal const string None = @"";
    internal const string Scheduled = @"SCHEDULED";
    internal const string InProgress = @"IN PROGRESS";
    internal const string Canceled = @"CANCELED";
    internal const string Completed = @"COMPLETED";
}
