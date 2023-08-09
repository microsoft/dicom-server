// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.CompleteUpdateStudyAsync"/>
/// </summary>
public sealed class CompleteStudyArguments
{
    public int PartitionKey { get; }

    public IReadOnlyList<InstanceMetadata> InstanceMetadataList { get; }

    public string StudyInstanceUid { get; }

    public string ChangeDataset { get; set; }

    public CompleteStudyArguments(int partitionKey, string studyInstanceUid, string dicomDataset)
        : this(partitionKey, studyInstanceUid, dicomDataset, new List<InstanceMetadata>())
    {
    }

    public CompleteStudyArguments(int partitionKey, string studyInstanceUid, string dicomDataset, IReadOnlyList<InstanceMetadata> instanceMetadataList)
    {
        PartitionKey = partitionKey;
        StudyInstanceUid = studyInstanceUid;
        ChangeDataset = dicomDataset;
        InstanceMetadataList = instanceMetadataList;
    }
}
