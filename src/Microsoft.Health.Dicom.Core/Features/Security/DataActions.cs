// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Health.Dicom.Core.Features.Security
{
    [Flags]
    [SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Reserve additional bits for actions.")]
    public enum DataActions : ulong
    {
        [EnumMember(Value = "none")]
        None = 0,

        [EnumMember(Value = "read")]
        Read = 1,

        [EnumMember(Value = "write")]
        Write = 1 << 1,

        [EnumMember(Value = "delete")]
        Delete = 1 << 2,

        [EnumMember(Value = "manageExtendedQueryTags")]
        ManageExtendedQueryTags = 1 << 3,    // Allow to manage extended query tags.

        [EnumMember(Value = "*")]
        All = (ManageExtendedQueryTags << 1) - 1,
    }
}
