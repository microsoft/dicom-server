// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models
{
    /// <summary>
    /// Representing the Procedure Step State.
    /// </summary>
    public static class ProcedureStepState
    {
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentState"></param>
        /// <param name="futureState"></param>
        /// <returns></returns>
        public static bool CanTransition(string currentState, string futureState)
        {
            // Starting state must be "Scheduled"
            if (string.IsNullOrWhiteSpace(currentState) && string.Equals(currentState, Scheduled, StringComparison.Ordinal))
            {
                return true;
            }

            // Before the UPS is set to the final state, it must be set to InProgress
            if (string.Equals(currentState, Scheduled, StringComparison.Ordinal) &&
                string.Equals(futureState, InProgress, StringComparison.Ordinal))
            {
                return true;
            }

            // The SCP does not permit the status of a SCHEDULED UPS to be set to COMPLETED or CANCELED without first being set to IN PROGRESS
            if (string.Equals(currentState, InProgress, StringComparison.Ordinal) &&
                (string.Equals(futureState, Completed, StringComparison.Ordinal) ||
                 string.Equals(futureState, Canceled, StringComparison.Ordinal)))
            {
                return true;
            }

            return false;
        }
    }
}
