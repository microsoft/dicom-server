// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Query status of query tag.
    /// </summary>
    [SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Vaule is stroed in SQL as TINYINT")]
    public enum QueryTagQueryStatus : byte
    {
        /// <summary>
        /// The tag is allowed to be queried.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// The tag is not allowed to be queried.
        /// </summary>
        Disabled = 0,
    }
}
