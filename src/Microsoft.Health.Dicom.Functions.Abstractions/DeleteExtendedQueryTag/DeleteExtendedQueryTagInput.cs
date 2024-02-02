// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public class DeleteExtendedQueryTagInput
{
    /// <summary>
    /// The tag path to delete
    /// </summary>
    public string TagPath { get; set; }

    /// <summary>
    /// The tag key to delete
    /// </summary>
    public int TagKey { get; set; }

    /// <summary>
    /// The VR
    /// </summary>
    public string VR { get; set; }

    /// <summary>
    /// Gets or sets the options that configure how the operation batches groups of DICOM query tag instances for deletion.
    /// </summary>
    /// <value>A set of delete xqt batching options.</value>
    public BatchingOptions Batching { get; set; }
}
