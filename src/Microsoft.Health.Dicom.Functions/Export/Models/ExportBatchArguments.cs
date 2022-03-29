// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Export;

namespace Microsoft.Health.Dicom.Functions.Export.Models;

public class ExportBatchArguments
{
    public IExportBatch Batch { get; set; }

    public ExportSinkDescription SinkDescription { get; set; }
}
