// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public class UpdateInstanceWatermarkArguments
{
    public Partition Partition { get; }

    [Obsolete("To be removed with V1 cleanup.")]
    public int PartitionKey { get; }

    public string StudyInstanceUid { get; }

    [Obsolete("To be removed with V1 cleanup.")]
    public UpdateInstanceWatermarkArguments(int partitionKey, string studyInstanceUid)
        : this(new Partition(partitionKey, Partition.UnknownName), studyInstanceUid)
    {
        PartitionKey = partitionKey;
    }

    [JsonConstructor]
    public UpdateInstanceWatermarkArguments(Partition partition, string studyInstanceUid)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        StudyInstanceUid = EnsureArg.IsNotEmptyOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
    }
}
