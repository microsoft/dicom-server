// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

[Flags]
internal enum ExportPatternPlaceholders : sbyte
{
    None = 0x0,
    Operation = 0x1,
    Study = 0x2,
    Series = 0x4,
    SopInstance = 0x8,
    All = ~None,
}
