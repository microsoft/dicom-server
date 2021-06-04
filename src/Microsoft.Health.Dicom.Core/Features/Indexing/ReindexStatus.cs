// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// The reindex status.
    /// </summary>
    public enum ReindexStatus
    {
        /// <summary>
        /// Reindex is ongoing.
        /// </summary>
        Processing = 0,

        /// <summary>
        /// Reindex is paused.
        /// </summary>
        Paused = 1,

        /// <summary>
        /// Reindex is completed.
        /// </summary>
        Completed = 2,
    }
}
