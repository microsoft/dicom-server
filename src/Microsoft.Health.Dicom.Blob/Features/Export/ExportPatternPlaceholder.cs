// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

[Flags]
internal enum ExportPatternPlaceholders
{
    None = 0x0,
    Operation = 0x1,
    Study = 0x2,
    Series = 0x4,
    SopInstance = 0x8,
    All = Operation | Study | Series | SopInstance,
}
