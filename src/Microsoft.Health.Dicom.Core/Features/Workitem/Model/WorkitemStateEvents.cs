// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Workitem state events
    /// </summary>
    public enum WorkitemStateEvents
    {
        NCreate,
        NActionToInProgressWithCorrectTransactionUID,
        NActionToInProgressWithoutCorrectTransactionUID,
        NActionToScheduled,
        NActionToCompletedWithCorrectTransactionUID,
        NActionToCompletedWithoutCorrectTransactionUID,
        NActionToRequestCancel,
        NActionToCanceledWithCorrectTransactionUID,
        NActionToCanceledWithoutCorrectTransactionUID
    }
}
