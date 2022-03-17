// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Operations;

internal sealed class NullOperationStatus : ICustomOperationStatus
{
    public static ICustomOperationStatus Value { get; } = new NullOperationStatus();

    private NullOperationStatus()
    { }

    public DateTime? CreatedTime => null;

    public OperationProgress GetProgress()
        => new OperationProgress();
}
