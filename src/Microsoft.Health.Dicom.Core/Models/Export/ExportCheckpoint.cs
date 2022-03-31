// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class ExportCheckpoint : ExportInput, IOperationCheckpoint
{
    public ExportResult Result { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int PercentComplete => 0;

    public IReadOnlyCollection<string> ResourceIds => null;
}
