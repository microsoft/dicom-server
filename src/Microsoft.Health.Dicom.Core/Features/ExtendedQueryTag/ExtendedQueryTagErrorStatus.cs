// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Status of extended query tag error.
    /// </summary>
    public enum ExtendedQueryTagErrorStatus
    {
        /// <summary>
        /// The extended query tag error is not acknowledged.
        /// </summary>
        Unacknowledged = 0,

        /// <summary>
        /// The extended query tag error is acknowledged.
        /// </summary>
        Acknowledged = 1,
    }
}
