// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Specifies the category of a DICOM operation.
/// </summary>
public enum DicomOperation
{
    /// <summary>
    /// Specifies an data cleanup operation that cleans up instance data.
    /// </summary>
    DataCleanup,

    /// <summary>
    /// Specifies an content length backfill operation.
    /// </summary>
    ContentLengthBackFill,

    /// <summary>
    /// Specifies an export operation that copies data out of the DICOM server and into an external data store.
    /// </summary>
    Export,

    /// <summary>
    /// Specifies a reindexing operation that updates the indicies for previously added data based on new tags.
    /// </summary>
    Reindex,

    /// <summary>
    /// Specifies an operation whose type is missing or unrecognized.
    /// </summary>
    Unknown,

    /// <summary>
    /// Specifies an update operation that updates the Dicom attributes.
    /// </summary>
    Update,
}
