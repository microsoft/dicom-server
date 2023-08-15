// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.UpdateInstanceBlobsAsync"/>
/// </summary>
[Obsolete("To be removed with V1 cleanup.")]
public sealed class UpdateInstanceBlobArguments
{
    public int PartitionKey { get; }

    [Obsolete("To be removed with V1 cleanup.")]
    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }

    public string ChangeDataset { get; }

    [Obsolete("To be removed with V1 cleanup.")]
    public UpdateInstanceBlobArguments(int partitionKey, IReadOnlyList<InstanceFileState> instanceWatermarks, string changeDataset)
    {
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
        PartitionKey = partitionKey;
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
    }
}
