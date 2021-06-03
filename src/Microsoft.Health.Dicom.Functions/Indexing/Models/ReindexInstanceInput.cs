// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    ///  Represents input to <see cref="ReindexOperation.ReindexInstanceAsync"/>
    /// </summary>
    public class ReindexInstanceInput
    {
        /// <summary>
        /// Gets or sets the inclusive end watermark.
        /// </summary>
        public long StartWatermark { get; set; }

        public long EndWatermark { get; set; }

        /// <summary>
        /// Gets or sets the tags upon which to index.
        /// </summary>
        public IReadOnlyList<ExtendedQueryTagStoreEntry> TagEntries { get; set; }

        public override string ToString()
        {
            string tagEntriesText = string.Concat(",", TagEntries.Select(x => x.ToString()));
            return $"StartWatermark - { StartWatermark}, EndWatermark - {EndWatermark},  TagEntries - {tagEntriesText}";
        }

    }
}
