// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs;

public class BlobMigrationConfiguration
{
    /// <summary>
    /// Gets or sets the blob format type to write or read blobs
    /// </summary>
    public BlobMigrationFormatType FormatType { get; set; }

    /// <summary>
    /// Gets or sets flag to start blob duplication
    /// </summary>
    public bool StartDuplication { get; set; }
}
