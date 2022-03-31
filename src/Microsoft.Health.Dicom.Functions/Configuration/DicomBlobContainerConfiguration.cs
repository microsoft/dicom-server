// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Configuration;

internal class DicomBlobContainerConfiguration
{
    public const string SectionName = "Containers";

    [Required]
    public string Blob { get; set; }

    [Required]
    public string Metadata { get; set; }
}
