// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Features.Workitem.Model
{
    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Value is stored in SQL as TINYINT.")]
    public enum WorkitemStoreStatus : byte
    {
        /// Workitem being created
        None = 0,

        /// Workitem created or updated
        ReadWrite = 1,

        /// Workitem being updated/deleted, etc.
        Read = 2
    }
}
