// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.UpdateInstanceBlobsAsync"/>
/// </summary>
public sealed class UpdateInstanceBlobArguments
{
    public int PartitionKey { get; }

    public Partition Partition { get; set; }

    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }

    public IReadOnlyList<InstanceMetadata> InstanceMetadatas { get; }

    public string ChangeDataset { get; }

    public UpdateInstanceBlobArguments(int partitionKey, IReadOnlyList<InstanceFileState> instanceWatermarks, string changeDataset)
        : this(new Partition(partitionKey, Partition.UnknownName), new List<InstanceMetadata>(), changeDataset)
    {
        PartitionKey = partitionKey;
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
    }

    public UpdateInstanceBlobArguments(Partition partition, IReadOnlyList<InstanceMetadata> instances, string changeDataset)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        InstanceMetadatas = EnsureArg.IsNotNull(instances, nameof(instances));
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
    }
}
