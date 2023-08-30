// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public sealed class CleanupBlobArguments
{
    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }
    public Partition Partition { get; }

    public CleanupBlobArguments(IReadOnlyList<InstanceFileState> instanceWatermarks, Partition partition)
    {
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
    }
}
