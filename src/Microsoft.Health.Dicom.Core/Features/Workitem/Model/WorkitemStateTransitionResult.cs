// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Workitem state transition result
    /// </summary>
    public class WorkitemStateTransitionResult
    {
        public WorkitemStateTransitionResult(ProcedureStepState state, string code, bool isError)
        {
            State = state;
            Code = code;
            IsError = isError;
        }

        public ProcedureStepState State { get; }

        public string Code { get; }

        public bool IsError { get; }

        public bool HasWarningWithCode => !IsError && !string.IsNullOrWhiteSpace(Code);
    }
}
