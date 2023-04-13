// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
///  Represents input to <see cref="UpdateDurableFunction.UpdateInstanceBatchAsync"/>
/// </summary>
public sealed class BatchUpdateArguments
{
    public int PartitionKey { get; }

    public IReadOnlyList<InstanceFileIdentifier> InstanceWatermarks { get; }

    public string ChangeDataset { get; set; }

    public BatchUpdateArguments(int partitionKey, IReadOnlyList<InstanceFileIdentifier> instanceWatermarks, string changeDataset)
    {
        PartitionKey = partitionKey;
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
    }
}
