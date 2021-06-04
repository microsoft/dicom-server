// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    /// Represents input to activity <see cref="ReindexDurableFunction.UpdateReindexProgressActivityAsync"/>.
    /// </summary>
    public class UpdateReindexProgressActivityInput
    {
        /// <summary>
        /// Gets or sets the end watermark of operation.
        /// </summary>
        public long EndWatermark { get; set; }

        ///// <summary>
        ///// Gets or sets the operation id.
        ///// </summary>
        public string OperationId { get; set; }
    }
}
