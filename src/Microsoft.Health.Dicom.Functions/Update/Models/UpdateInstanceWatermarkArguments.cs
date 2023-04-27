// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.UpdateInstanceWatermarkAsync"/>
/// </summary>
public class UpdateInstanceWatermarkArguments
{
    public int PartitionKey { get; }

    public string StudyInstanceUid { get; }

    public UpdateInstanceWatermarkArguments(int partitionKey, string studyInstanceUid)
    {
        PartitionKey = partitionKey;
        StudyInstanceUid = studyInstanceUid;
    }
}
