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
    public Partition Partition { get; set; }

    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }

    public string ChangeDataset { get; }

    public UpdateInstanceBlobArguments(Partition partition, IReadOnlyList<InstanceFileState> instanceWatermarks, string changeDataset)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
    }
}
