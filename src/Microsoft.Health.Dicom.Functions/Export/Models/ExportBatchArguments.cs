// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Functions.Export.Models;

public class ExportBatchArguments
{
    public long Offset { get; set; }

    public BatchOptions Batching { get; set; }

    public DataSource Source { get; set; }

    public ExportLocation Destination { get; set; }
}
