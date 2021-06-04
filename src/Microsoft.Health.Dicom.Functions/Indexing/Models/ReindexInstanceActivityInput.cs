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
    ///  Represents input to <see cref="ReindexDurableFunction.ReindexInstanceActivityAsync"/>
    /// </summary>
    public class ReindexInstanceActivityInput
    {
        /// <summary>
        /// Gets or sets the inclusive start watermark.
        /// </summary>
        public long StartWatermark { get; set; }

        /// <summary>
        /// Gets or sets the inclusive end watermark.
        /// </summary>
        public long EndWatermark { get; set; }

        /// <summary>
        /// Gets or sets the tag entires.
        /// </summary>
        public IEnumerable<ExtendedQueryTagStoreEntry> TagStoreEntries { get; set; }
    }
}
