// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    ///  Represents input to <see cref="ReindexDurableFunction.ReindexInstancesAsync"/>
    /// </summary>
    public class ReindexInstanceInput
    {
        /// <summary>
        /// Gets or sets the inclusive watermark range.
        /// </summary>
        public WatermarkRange WatermarkRange { get; set; }

        /// <summary>
        /// Gets or sets the tag entires.
        /// </summary>
        public IReadOnlyCollection<ExtendedQueryTagStoreEntry> TagStoreEntries { get; set; }
    }
}
