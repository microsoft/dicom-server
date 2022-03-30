// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class PaginatedResults<T>
{
    public T Result { get; init; }

    public ContinuationToken? ContinuationToken { get; init; }
}
