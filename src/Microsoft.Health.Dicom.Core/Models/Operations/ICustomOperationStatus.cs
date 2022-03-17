// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Operations;

internal interface ICustomOperationStatus
{
    DateTime? CreatedTime { get; }

    OperationProgress GetProgress();
}
