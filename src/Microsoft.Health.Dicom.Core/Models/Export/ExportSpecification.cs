// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class ExportSpecification
{
    [Required]
    public ExportManifest Source { get; set; }

    [Required]
    public ExportDestination Destination { get; set; }
}
