// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class ExportIdentifiersInput
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<DicomIdentifier> Identifiers { get; set; }

    [Required]
    public ExportDestination Destination { get; set; }
}
