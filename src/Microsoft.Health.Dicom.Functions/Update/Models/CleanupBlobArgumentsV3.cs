// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public sealed class CleanupBlobArgumentsV3
{
    public IReadOnlyList<InstanceMetadata> Instances { get; }
    public Partition Partition { get; }

    public CleanupBlobArgumentsV3(IReadOnlyList<InstanceMetadata> instances, Partition partition)
    {
        Instances = EnsureArg.IsNotNull(instances, nameof(instances));
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
    }
}