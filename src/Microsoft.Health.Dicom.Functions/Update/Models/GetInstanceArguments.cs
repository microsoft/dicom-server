// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
///  Represents input to <see cref="UpdateDurableFunction.GetInstanceWatermarksInStudyAsync"/>
/// </summary>
public sealed class GetInstanceArguments
{
    public int PartitionKey { get; }
    public string StudyInstanceUid { get; }

    public GetInstanceArguments(int partitionKey, string studyInstanceUid)
    {
        PartitionKey = partitionKey;
        StudyInstanceUid = studyInstanceUid;
    }
}
