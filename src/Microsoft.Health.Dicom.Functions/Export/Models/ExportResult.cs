// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Export.Models;

public class ExportResult
{
    public bool IsEmpty => Exported == 0 && Failed == 0;

    public int Exported { get; set; }

    public int Failed { get; set; }
}
