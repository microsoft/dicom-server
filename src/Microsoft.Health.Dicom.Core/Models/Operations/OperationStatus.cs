// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Operations
{
    public enum OperationStatus
    {
        Unknown,
        Pending,
        Running,
        Completed,
        Failed,
        Canceled,
    }
}
