// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models.Operations;

/// <summary>
/// Specifies the category of a DICOM operation.
/// </summary>
public enum DicomOperation
{
    /// <summary>
    /// Specifies an operation whose type is missing or unrecognized.
    /// </summary>
    Unknown,

    /// <summary>
    /// Specifies a reindexing operation that updates the indicies for previously added data based on new tags.
    /// </summary>
    Reindex,

    /// <summary>
    /// Specifies an export operation that copies data from the DICOM server to some other storage.
    /// </summary>
    Export,
}
