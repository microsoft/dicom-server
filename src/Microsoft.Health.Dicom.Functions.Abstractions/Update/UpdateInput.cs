// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Functions.Update;

public class UpdateInput
{
    public Partition Partition { get; set; }

    public int PartitionKey { get; set; }

    public IReadOnlyList<string> StudyInstanceUids { get; set; }

    public string ChangeDataset { get; set; }
}
