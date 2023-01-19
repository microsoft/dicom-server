// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store;

public sealed class StoreErrorResult
{
    public StoreErrorResult(string error, bool isRequiredTag)
    {
        Error = error;
        IsRequiredCoreTag = isRequiredTag;
    }

    public string Error { get; }

    public bool IsRequiredCoreTag { get; }
}
