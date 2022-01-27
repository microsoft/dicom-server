// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

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


        public static bool CanTransition(WorkitemStateEvents action, string state, out string errorOrWarningCode)
        {
            errorOrWarningCode = GetStateCode(action, state);
            return string.IsNullOrEmpty(errorOrWarningCode);
        }

        /// <summary>
        /// The method returns the valid transitiion according to the spec
        /// https://dicom.nema.org/dicom/2013/output/chtml/part04/chapter_CC.html#table_CC.1.1-2
        /// </summary>
        /// <param name="action"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static string GetStateCode(WorkitemStateEvents action, string state) => (action, state) switch
        {
            (WorkitemStateEvents.NCreate, "") => string.Empty,
            (WorkitemStateEvents.NCreate, Scheduled) => Error0111,
            (WorkitemStateEvents.NCreate, InProgress) => Error0111,
            (WorkitemStateEvents.NCreate, Completed) => Error0111,
            (WorkitemStateEvents.NCreate, Canceled) => Error0111,

            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, "") => ErrorC307,
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, Scheduled) => string.Empty,
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, InProgress) => ErrorC302,
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, Completed) => ErrorC300,
            (WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID, Canceled) => ErrorC300,

            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, "") => ErrorC307,
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, Scheduled) => ErrorC301,
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, InProgress) => ErrorC301,
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, Completed) => ErrorC301,
            (WorkitemStateEvents.NActionToInProgressWithoutCorrectTransactionUID, Canceled) => ErrorC301,

            (WorkitemStateEvents.NActionToScheduled, "") => ErrorC307,
            (WorkitemStateEvents.NActionToScheduled, Scheduled) => ErrorC303,
            (WorkitemStateEvents.NActionToScheduled, InProgress) => ErrorC303,
            (WorkitemStateEvents.NActionToScheduled, Completed) => ErrorC303,
            (WorkitemStateEvents.NActionToScheduled, Canceled) => ErrorC303,

            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, "") => ErrorC307,
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, Scheduled) => ErrorC310,
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, InProgress) => string.Empty,
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, Completed) => WarningB306,
            (WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID, Canceled) => ErrorC300,

            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, "") => ErrorC307,
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, Scheduled) => ErrorC301,
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, InProgress) => ErrorC301,
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, Completed) => ErrorC301,
            (WorkitemStateEvents.NActionToCompletedWithoutCorrectTransactionUID, Canceled) => ErrorC301,

            (WorkitemStateEvents.NActionToRequestCancel, "") => ErrorC307,
            (WorkitemStateEvents.NActionToRequestCancel, Scheduled) => string.Empty,
            (WorkitemStateEvents.NActionToRequestCancel, InProgress) => string.Empty,
            (WorkitemStateEvents.NActionToRequestCancel, Completed) => ErrorC311,
            (WorkitemStateEvents.NActionToRequestCancel, Canceled) => WarningB304,

            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, "") => ErrorC307,
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, Scheduled) => ErrorC310,
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, InProgress) => string.Empty,
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, Completed) => ErrorC300,
            (WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID, Canceled) => WarningB304,

            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, "") => string.Empty,
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, Scheduled) => ErrorC301,
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, InProgress) => ErrorC301,
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, Completed) => ErrorC301,
            (WorkitemStateEvents.NActionToCanceledWithoutCorrectTransactionUID, Canceled) => ErrorC301,

            _ => throw new NotImplementedException()
        };
    }
}
