// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Representing the Procedure Step State.
    /// </summary>
    public static class ProcedureStepState
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
        /// The UPS is scheduled to be performed.
        /// </summary>
        public const string Scheduled = @"SCHEDULED";

        /// <summary>
        /// The UPS has been claimed and a Locking UID has been set. Performance of the UPS has likely started.
        /// </summary>
        public const string InProgress = @"IN PROGRESS";

        /// <summary>
        /// The UPS has been completed.
        /// </summary>
        public const string Completed = @"COMPLETED";

        /// <summary>
        /// The UPS has been permanently stopped before or during performance of the step.
        /// This may be due to voluntary or involuntary action by a human or machine.
        /// Any further UPS-driven work required to complete the scheduled task must be performed by scheduling another (different) UPS.
        /// </summary>
        public const string Canceled = @"CANCELED";


        public static WorkitemStateTransitionResult GetTransitionState(WorkitemStateEvents action, string state)
        {
            return CheckProcedureStepStateTransitionTable(action, state ?? string.Empty);
        }

        /// <summary>
        /// The method returns the valid transitiion according to the spec
        /// https://dicom.nema.org/dicom/2013/output/chtml/part04/chapter_CC.html#table_CC.1.1-2
        /// </summary>
        /// <param name="action"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static WorkitemStateTransitionResult CheckProcedureStepStateTransitionTable(WorkitemStateEvents action, string state) => (action, state) switch
        {
            (WorkitemStateEvents.NCreate, "") => new WorkitemStateTransitionResult(Scheduled, null, false),
            (WorkitemStateEvents.NCreate, Scheduled) => new WorkitemStateTransitionResult(null, Error0111, true),
            (WorkitemStateEvents.NCreate, InProgress) => new WorkitemStateTransitionResult(null, Error0111, true),
            (WorkitemStateEvents.NCreate, Completed) => new WorkitemStateTransitionResult(null, Error0111, true),
            (WorkitemStateEvents.NCreate, Canceled) => new WorkitemStateTransitionResult(null, Error0111, true),

            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, Scheduled) => new WorkitemStateTransitionResult(InProgress, null, false),
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, InProgress) => new WorkitemStateTransitionResult(null, ErrorC302, true),
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, Completed) => new WorkitemStateTransitionResult(null, ErrorC300, true),
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, Canceled) => new WorkitemStateTransitionResult(null, ErrorC300, true),

            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, Scheduled) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, InProgress) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, Completed) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, Canceled) => new WorkitemStateTransitionResult(null, ErrorC301, true),

            (WorkitemStateEvents.NActionToScheduled, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToScheduled, Scheduled) => new WorkitemStateTransitionResult(null, ErrorC303, true),
            (WorkitemStateEvents.NActionToScheduled, InProgress) => new WorkitemStateTransitionResult(null, ErrorC303, true),
            (WorkitemStateEvents.NActionToScheduled, Completed) => new WorkitemStateTransitionResult(null, ErrorC303, true),
            (WorkitemStateEvents.NActionToScheduled, Canceled) => new WorkitemStateTransitionResult(null, ErrorC303, true),

            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, Scheduled) => new WorkitemStateTransitionResult(null, ErrorC310, true),
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, InProgress) => new WorkitemStateTransitionResult(Completed, null, false),
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, Completed) => new WorkitemStateTransitionResult(null, WarningB306, false),
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, Canceled) => new WorkitemStateTransitionResult(null, ErrorC300, true),

            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, Scheduled) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, InProgress) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, Completed) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, Canceled) => new WorkitemStateTransitionResult(null, ErrorC301, true),

            (WorkitemStateEvents.NActionToRequestCancel, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToRequestCancel, Scheduled) => new WorkitemStateTransitionResult(Canceled, null, false),

            // This case returns Error, with a message, because we do not support notifying the owner of the workitem instance about the cancellation request.
            (WorkitemStateEvents.NActionToRequestCancel, InProgress) => new WorkitemStateTransitionResult(null, ErrorC312, true),
            (WorkitemStateEvents.NActionToRequestCancel, Completed) => new WorkitemStateTransitionResult(null, ErrorC311, true),
            (WorkitemStateEvents.NActionToRequestCancel, Canceled) => new WorkitemStateTransitionResult(null, WarningB304, false),

            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, Scheduled) => new WorkitemStateTransitionResult(null, ErrorC310, true),
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, InProgress) => new WorkitemStateTransitionResult(null, string.Empty, false),
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, Completed) => new WorkitemStateTransitionResult(null, ErrorC300, true),
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, Canceled) => new WorkitemStateTransitionResult(null, WarningB304, false),

            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, "") => new WorkitemStateTransitionResult(null, ErrorC307, true),
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, Scheduled) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, InProgress) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, Completed) => new WorkitemStateTransitionResult(null, ErrorC301, true),
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, Canceled) => new WorkitemStateTransitionResult(null, ErrorC301, true),

            _ => throw new Exceptions.NotSupportedException(string.Format(
                            CultureInfo.InvariantCulture,
                            DicomCoreResource.InvalidProcedureStepStateTransition,
                            string.Empty,
                            state,
                            string.Empty))
        };
    }
}
