// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public class ExportInput
{
    public TypedConfiguration<ExportSourceType> Source { get; set; }

    public TypedConfiguration<ExportDestinationType> Destination { get; set; }

    public BatchingOptions Batching { get; set; }
}
