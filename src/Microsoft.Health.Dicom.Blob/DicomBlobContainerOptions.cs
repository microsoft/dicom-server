// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Blob;

/// <summary>
/// Represents the various Azure Blob Containers used by the DICOM server.
/// </summary>
public sealed class DicomBlobContainerOptions
{
    public const string SectionName = "Containers";

    /// <summary>
    /// Gets or sets the container name for metadata.
    /// </summary>
    /// <value>The metadata container name.</value>
    [Required]
    public string Metadata { get; set; }

    /// <summary>
    /// Gets or sets the container name for files.
    /// </summary>
    /// <value>The file container name.</value>
    [Required]
    public string File { get; set; }

    /// <summary>
    /// Gets or sets the container name for system.
    /// </summary>
    /// <value>The file container name.</value>
    [Required]
    public string System { get; set; }
}
