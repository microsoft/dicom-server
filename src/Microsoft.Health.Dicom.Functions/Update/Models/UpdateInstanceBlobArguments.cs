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
public sealed class UpdateInstanceBlobArguments
{
    public IReadOnlyList<InstanceFileState> InstanceWatermarks { get; }

    public string InputIdentifier { get; }

    public int StudyInstanceIndex { get; }

    public UpdateInstanceBlobArguments(string inputIdentifier, IReadOnlyList<InstanceFileState> instanceWatermarks, int studyInstanceIndex)
    {
        InstanceWatermarks = EnsureArg.IsNotNull(instanceWatermarks, nameof(instanceWatermarks));
        InputIdentifier = EnsureArg.IsNotNull(inputIdentifier, nameof(inputIdentifier));
        StudyInstanceIndex = studyInstanceIndex;
    }
}
