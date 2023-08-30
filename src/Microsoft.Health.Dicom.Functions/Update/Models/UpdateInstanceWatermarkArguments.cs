// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

[Obsolete("To be removed with V1 cleanup.")]
public class UpdateInstanceWatermarkArguments
{

    [Obsolete("To be removed with V1 cleanup.")]
    public int PartitionKey { get; }

    public string StudyInstanceUid { get; }

    [Obsolete("To be removed with V1 cleanup.")]
    public UpdateInstanceWatermarkArguments(int partitionKey, string studyInstanceUid)
    {
        PartitionKey = partitionKey;
        StudyInstanceUid = EnsureArg.IsNotEmptyOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
    }

}
