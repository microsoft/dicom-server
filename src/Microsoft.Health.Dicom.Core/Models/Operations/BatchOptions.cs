// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models.Operations;

public class BatchOptions
{
    public int Size { get; set; }

    public int MaxParallel { get; set; }

    public int MaxParallelValues => Size * MaxParallel;
}
