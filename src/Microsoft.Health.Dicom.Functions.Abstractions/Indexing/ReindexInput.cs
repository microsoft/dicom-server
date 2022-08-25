// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Indexing;

/// <summary>
/// Represents the input to the reindexing operation.
/// </summary>
public class ReindexInput
{
    /// <summary>
    /// Gets or sets the collection of newly added extended query tag keys to be reindexed.
    /// </summary>
    /// <value>A collection of one or more keys for extended query tags in the store.</value>
    public IReadOnlyCollection<int> QueryTagKeys { get; set; }

    /// <summary>
    /// Gets or sets the options that configure how the operation batches groups of DICOM SOP instances for reindexing.
    /// </summary>
    /// <value>A set of reindexing batching options.</value>
    public BatchingOptions Batching { get; set; }
}
