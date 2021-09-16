// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Models
{
    /// <summary>
    /// Representing the index status.
    /// </summary>
    [SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Value is stored in SQL as TINYINT.")]
    public enum IndexStatus : byte
    {
        Creating = 0,

        Created = 1,
    }
}
