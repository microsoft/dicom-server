// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Status of extended query tag.
    /// </summary>
    public enum ExtendedQueryTagStatus
    {
        /// <summary>
        /// The query tag is being reindexed.
        /// </summary>
        Reindexing = 0,

        /// <summary>
        /// The query tag has been added to system.
        /// </summary>
        Added = 1,

        /// <summary>
        /// The query tag is being deindexed.
        /// </summary>
        Deindexing = 2,
    }
}
