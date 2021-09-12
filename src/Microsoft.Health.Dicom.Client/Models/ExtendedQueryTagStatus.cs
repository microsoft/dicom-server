// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Status of extended query tag.
    /// </summary>
    [SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Value is stored in SQL as TINYINT.")]
    public enum ExtendedQueryTagStatus : byte
    {
        /// <summary>
        /// The query tag is being added.
        /// </summary>
        Adding = 0,

        /// <summary>
        /// The query tag has been added to system.
        /// </summary>
        Ready = 1,

        /// <summary>
        /// The query tag is being deleting.
        /// </summary>
        Deleting = 2,
    }
}
