// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public class BaseArguments
{
    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }

    public int PartitionKey { get; }

    public BaseArguments(IReadOnlyList<InstanceFileState> instanceWatermarks, int partitionKey = default)
    {
        PartitionKey = partitionKey;
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
    }
}