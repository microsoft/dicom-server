// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Configs;

public class BlobMigrationConfiguration
{
    /// <summary>
    /// Gets or sets the blob format type to write or read blobs
    /// </summary>
    public BlobMigrationFormatType FormatType { get; set; }

    /// <summary>
    /// Gets or sets flag to start blob migration
    /// </summary>
    public bool StartCopy { get; set; }

    /// <summary>
    /// Gets or sets flag to start delete old format blobs
    /// </summary>
    public bool StartDelete { get; set; }

    /// <summary>
    /// Gets or sets a flag to log usage of the old blob format
    /// </summary>
    public bool LogOldFormatUsage { get; set; }

    /// <summary>
    /// Gets or sets the copy files operation id
    /// </summary>
    public Guid CopyOperationId { get; set; } = Guid.Parse("1d4689da-ca3b-4659-b0c7-7bf6c9ff25e1");

    /// <summary>
    /// Gets or sets the delete files operation id
    /// </summary>
    public Guid DeleteOperationId { get; set; } = Guid.Parse("ce38a27e-b194-4645-b47a-fe91c38c330f");
}
