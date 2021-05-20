// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Entry of TagReindexOperationStore.
    /// </summary>
    public class TagReindexOperationEntry
    {
        /// <summary>
        /// The tag key.
        /// </summary>
        public long TagKey { get; set; }

        /// <summary>
        /// The operation id.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// The end wartermark.
        /// </summary>
        public long EndWatermark { get; set; }

        /// <summary>
        /// The tag status on operation.
        /// </summary>
        public TagOperationStatus Status { get; set; }

        /// <summary>
        /// The tag store entry.
        /// </summary>
        public ExtendedQueryTagStoreEntry TagStoreEntry { get; set; }
    }
}
