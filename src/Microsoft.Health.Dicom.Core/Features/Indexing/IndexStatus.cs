// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// The index status.
    /// </summary>
    public enum IndexStatus
    {
        /// <summary>
        /// index is ongoing.
        /// </summary>
        Processing = 0,

        /// <summary>
        /// Index is paused.
        /// </summary>
        Paused = 1,

        /// <summary>
        /// Reindex is completed.
        /// </summary>
        Completed = 2,
    }
}
