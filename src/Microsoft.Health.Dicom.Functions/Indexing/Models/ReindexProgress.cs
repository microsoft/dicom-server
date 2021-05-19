// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    /// Represents incremental progress in the re-indexing of DICOM instances.
    /// </summary>
    public class ReindexProgress
    {
        /// <summary>
        /// Gets or sets the next watermark of the next instance to process.
        /// </summary>
        public long NextWatermark { get; set; }

        ///// <summary>
        ///// Gets or sets the number of new errors encountered.
        ///// </summary>
        public string OperationId { get; set; }
    }
}
