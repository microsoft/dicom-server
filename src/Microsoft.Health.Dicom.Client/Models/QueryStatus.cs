// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models
{
    public enum QueryStatus
    {
        /// <summary>
        /// The tag is not allowed to be queried.
        /// </summary>
        Disabled,

        /// <summary>
        /// The tag is allowed to be queried.
        /// </summary>
        Enabled,
    }
}
