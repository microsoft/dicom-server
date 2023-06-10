// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.UpdateInstanceBlobsAsync"/>
/// </summary>
public sealed class UpdateInstanceBlobArguments : BaseArguments
{
    public string ChangeDataset { get; }

    public UpdateInstanceBlobArguments(int partitionKey, IReadOnlyList<InstanceFileState> instanceWatermarks, string changeDataset)
        : base(instanceWatermarks, partitionKey)
    {
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
    }
}
