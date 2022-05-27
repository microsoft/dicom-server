// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Workitem action event
/// </summary>
public enum WorkitemActionEvent
{
    NCreate,
    NActionToInProgress,
    NActionToScheduled,
    NActionToCompleted,
    NActionToRequestCancel,
    NActionToCanceled
}
