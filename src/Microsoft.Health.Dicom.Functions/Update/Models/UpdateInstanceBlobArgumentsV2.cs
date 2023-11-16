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
/// Represents input to <see cref="UpdateDurableFunction.UpdateInstanceBlobsV3Async"/>
/// </summary>
public class UpdateInstanceBlobArgumentsV2
{
    public Partition Partition { get; set; }

    public IReadOnlyList<InstanceMetadata> InstanceMetadataList { get; }

    public string ChangeDataset { get; }

    public UpdateInstanceBlobArgumentsV2(Partition partition, IReadOnlyList<InstanceMetadata> instanceMetadataList, string changeDataset)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        InstanceMetadataList = EnsureArg.IsNotNull(instanceMetadataList, nameof(instanceMetadataList));
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
    }
}
