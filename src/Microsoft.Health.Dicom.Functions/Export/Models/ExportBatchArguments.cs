// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Functions.Export.Models;

public class ExportBatchArguments
{
    public SourceManifest Source { get; set; }

    public ExportDestination Destination { get; set; }
}
