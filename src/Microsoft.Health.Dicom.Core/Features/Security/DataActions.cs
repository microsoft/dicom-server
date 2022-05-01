// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Health.Dicom.Core.Features.Security;

[Flags]
[SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute", Justification = "False positive due to All member.")]
public enum DataActions : long
{
    [EnumMember(Value = "none")]
    None = 0x0,

    [EnumMember(Value = "read")]
    Read = 0x1,

    [EnumMember(Value = "write")]
    Write = 0x2,

    [EnumMember(Value = "delete")]
    Delete = 0x4,

    [EnumMember(Value = "manageExtendedQueryTags")]
    ManageExtendedQueryTags = 0x8,

    [EnumMember(Value = "export")]
    Export = 0x10,

    [EnumMember(Value = "*")]
    All = ~None
}
