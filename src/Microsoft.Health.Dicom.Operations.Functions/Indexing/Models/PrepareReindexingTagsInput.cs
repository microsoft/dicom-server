// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Operations.Functions.Indexing.Models
{
    /// <summary>
    ///  Represents input to <see cref="ReindexDurableFunction.PrepareReindexingTagsAsync"/>
    /// </summary>
    public class PrepareReindexingTagsInput
    {
        /// <summary>
        /// Gets or sets the operation id
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets the tag keys
        /// </summary>
        public IReadOnlyList<int> TagKeys { get; set; }

    }
}
