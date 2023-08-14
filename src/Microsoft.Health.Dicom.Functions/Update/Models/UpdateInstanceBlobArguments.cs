// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.UpdateInstanceBlobsAsync"/>
/// </summary>
public sealed class UpdateInstanceBlobArguments
{
    [Obsolete("To be removed with V1 cleanup.")]
    public int PartitionKey { get; }

    public Partition Partition { get; set; }

    [Obsolete("To be removed with V1 cleanup.")]
    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }

    public IReadOnlyList<InstanceMetadata> InstanceMetadataList { get; }

    public string ChangeDataset { get; }

    [Obsolete("To be removed with V1 cleanup.")]
    public UpdateInstanceBlobArguments(int partitionKey, IReadOnlyList<InstanceFileState> instanceWatermarks, string changeDataset)
        : this(new Partition(partitionKey, Partition.UnknownName), new List<InstanceMetadata>(), changeDataset)
    {
        PartitionKey = partitionKey;
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
    }

    [JsonConstructor]
    public UpdateInstanceBlobArguments(Partition partition, IReadOnlyList<InstanceMetadata> instances, string changeDataset)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        InstanceMetadataList = EnsureArg.IsNotNull(instances, nameof(instances));
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
    }
}
