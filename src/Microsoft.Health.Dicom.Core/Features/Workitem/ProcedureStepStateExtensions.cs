// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// The Procedure Step State Extension (Helper methods)
/// </summary>
public static class ProcedureStepStateExtensions
{
    private const string Error0111 = "0111";
    private const string WarningB304 = "B304";
    private const string WarningB306 = "B306";
    private const string ErrorC300 = "C300";
    private const string ErrorC301 = "C301";
    private const string ErrorC302 = "C302";
    private const string ErrorC303 = "C303";
    private const string ErrorC307 = "C307";
    private const string ErrorC310 = "C310";
    private const string ErrorC311 = "C311";
    private const string ErrorC312 = "C312";

    /// <summary>
    /// Gets the Transition State from Procedure Step State for the given Action
    /// </summary>
    /// <param name="state">The Procedure Step State</param>
    /// <param name="action">The Action being performed on a Workitem</param>
    /// <param name="hasMatchingTransactionUid"></param>
    /// <returns></returns>
    public static WorkitemStateTransitionResult GetTransitionState(this ProcedureStepState state, WorkitemActionEvent action, bool hasMatchingTransactionUid = true)
    {
        return state.CheckProcedureStepStateTransitionTable(action, hasMatchingTransactionUid);
    }

    /// <summary>
    /// Gets the display Name of a Procedure Step State
    /// </summary>
    /// <param name="state">The Procedure Step state</param>
    /// <returns>Returns the display name of the state</returns>
    public static string GetStringValue(this ProcedureStepState state)
    {
        return state switch
        {
            ProcedureStepState.None => ProcedureStepStateConstants.None,
            ProcedureStepState.Scheduled => ProcedureStepStateConstants.Scheduled,
            ProcedureStepState.InProgress => ProcedureStepStateConstants.InProgress,
            ProcedureStepState.Canceled => ProcedureStepStateConstants.Canceled,
            ProcedureStepState.Completed => ProcedureStepStateConstants.Completed,
            _ => throw new ArgumentOutOfRangeException(state.ToString()),
        };
    }

    /// <summary>
    /// Gets Procedure Step State from String
    /// </summary>
    /// <param name="procedureStepStateStringValue">The Procedure Step State as String</param>
    /// <returns></returns>
    public static ProcedureStepState GetProcedureStepState(string procedureStepStateStringValue)
    {
        if (string.IsNullOrWhiteSpace(procedureStepStateStringValue))
        {
            return ProcedureStepState.None;
        }

        foreach (var procedureStepState in Enum.GetValues<ProcedureStepState>())
        {
            var displayName = procedureStepState.GetStringValue();
            if (string.Equals(displayName, procedureStepStateStringValue, StringComparison.OrdinalIgnoreCase))
            {
                return procedureStepState;
            }
        }

        return ProcedureStepState.None;
    }

    /// <summary>
    /// Gets the procedure step state from the DicomDataset
    /// </summary>
    /// <param name="dataset">The DICOM dataset</param>
    /// <returns>Returns Procedure Step State</returns>
    public static ProcedureStepState GetProcedureStepState(this DicomDataset dataset)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        if (!dataset.TryGetString(DicomTag.ProcedureStepState, out var stringValue))
        {
            return ProcedureStepState.None;
        }

        return GetProcedureStepState(stringValue);
    }

    /// <summary>
    /// The method returns the valid transitiion according to the spec
    /// https://dicom.nema.org/dicom/2013/output/chtml/part04/chapter_CC.html#table_CC.1.1-2
    /// </summary>
    /// <param name="state">The Workitem's Current Procedure Step State</param>
    /// <param name="action">The target event/action type</param>
    /// <param name="hasMatchingTransactionUid"></param>
    /// <returns></returns>
    /// <exception cref="Exceptions.NotSupportedException"></exception>
    private static WorkitemStateTransitionResult CheckProcedureStepStateTransitionTable(this ProcedureStepState state, WorkitemActionEvent action, bool hasMatchingTransactionUid = true) => (action, state) switch
    {
        (WorkitemActionEvent.NCreate, ProcedureStepState.None) => new WorkitemStateTransitionResult(ProcedureStepState.Scheduled, null, false),
        (WorkitemActionEvent.NCreate, ProcedureStepState.Scheduled) => new WorkitemStateTransitionResult(ProcedureStepState.None, Error0111, true),
        (WorkitemActionEvent.NCreate, ProcedureStepState.InProgress) => new WorkitemStateTransitionResult(ProcedureStepState.None, Error0111, true),
        (WorkitemActionEvent.NCreate, ProcedureStepState.Completed) => new WorkitemStateTransitionResult(ProcedureStepState.None, Error0111, true),
        (WorkitemActionEvent.NCreate, ProcedureStepState.Canceled) => new WorkitemStateTransitionResult(ProcedureStepState.None, Error0111, true),

        (WorkitemActionEvent.NActionToInProgress, ProcedureStepState.None) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC307, true),
        (WorkitemActionEvent.NActionToInProgress, ProcedureStepState.Scheduled) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.InProgress, null, false)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToInProgress, ProcedureStepState.InProgress) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC302, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToInProgress, ProcedureStepState.Completed) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC300, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToInProgress, ProcedureStepState.Canceled) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC300, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),

        (WorkitemActionEvent.NActionToScheduled, ProcedureStepState.None) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC307, true),
        (WorkitemActionEvent.NActionToScheduled, ProcedureStepState.Scheduled) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC303, true),
        (WorkitemActionEvent.NActionToScheduled, ProcedureStepState.InProgress) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC303, true),
        (WorkitemActionEvent.NActionToScheduled, ProcedureStepState.Completed) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC303, true),
        (WorkitemActionEvent.NActionToScheduled, ProcedureStepState.Canceled) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC303, true),

        (WorkitemActionEvent.NActionToCompleted, ProcedureStepState.None) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC307, true),
        (WorkitemActionEvent.NActionToCompleted, ProcedureStepState.Scheduled) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC310, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToCompleted, ProcedureStepState.InProgress) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.Completed, null, false)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToCompleted, ProcedureStepState.Completed) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, WarningB306, false)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToCompleted, ProcedureStepState.Canceled) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC300, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),

        (WorkitemActionEvent.NActionToRequestCancel, ProcedureStepState.None) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC307, true),
        (WorkitemActionEvent.NActionToRequestCancel, ProcedureStepState.Scheduled) => new WorkitemStateTransitionResult(ProcedureStepState.Canceled, null, false),

        // This case returns Error, with a message, because we do not support notifying the owner of the workitem instance about the cancellation request.
        (WorkitemActionEvent.NActionToRequestCancel, ProcedureStepState.InProgress) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC312, true),
        (WorkitemActionEvent.NActionToRequestCancel, ProcedureStepState.Completed) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC311, true),
        (WorkitemActionEvent.NActionToRequestCancel, ProcedureStepState.Canceled) => new WorkitemStateTransitionResult(ProcedureStepState.None, WarningB304, false),

        (WorkitemActionEvent.NActionToCanceled, ProcedureStepState.None) => new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC307, true),
        (WorkitemActionEvent.NActionToCanceled, ProcedureStepState.Scheduled) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC310, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToCanceled, ProcedureStepState.InProgress) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, string.Empty, false)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToCanceled, ProcedureStepState.Completed) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC300, true)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),
        (WorkitemActionEvent.NActionToCanceled, ProcedureStepState.Canceled) => hasMatchingTransactionUid
            ? new WorkitemStateTransitionResult(ProcedureStepState.None, WarningB304, false)
            : new WorkitemStateTransitionResult(ProcedureStepState.None, ErrorC301, true),

        _ => throw new Exceptions.NotSupportedException(string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidProcedureStepStateTransition,
                        string.Empty,
                        state,
                        string.Empty,
                        string.Empty))
    };
}
