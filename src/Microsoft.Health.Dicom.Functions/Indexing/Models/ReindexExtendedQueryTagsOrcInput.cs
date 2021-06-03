// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    ///  Represents input to <see cref="ReindexOperation.AddExtendedQueryTagsAsync"/>
    /// </summary>
    public class ReindexExtendedQueryTagsOrcInput
    {
        /// <summary>
        /// Gets or sets the operation id
        /// </summary>
        public Microsoft.Health.Dicom.Core.Features.Indexing.ReindexOperation OperationEntry { get; set; }
    }
}
