// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    /// <summary>
    /// Represent each extended query tag entry has retrieved from the store.
    /// </summary>
    public class ReindexJobTagStoreEntry
    {
        /// <summary>
        /// Key of this extended query tag entry.
        /// </summary>
        public ExtendedQueryTagStoreEntry QueryTagStoreEntry { get; set; }

        /// <summary>
        /// Status of this tag.
        /// </summary>
        public long MaxWatermark { get; set; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public string JobId { get; set; }

        public ReindexJobTagStatus Status { get; set; }


    }
}
