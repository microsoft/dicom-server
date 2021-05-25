// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    ///  Represents input to <see cref="ReindexOperation.AddExtendedQueryTagsAsync"/>
    /// </summary>
    public class AddExtendedQueryTagsInput
    {
        /// <summary>
        /// Gets or sets the operation id
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets the tags upon which to index.
        /// </summary>
        public IEnumerable<AddExtendedQueryTagEntry> ExtendedQueryTagEntries { get; set; }

    }
}
