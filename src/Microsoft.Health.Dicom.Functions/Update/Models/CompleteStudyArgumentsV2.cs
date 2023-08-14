// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public class CompleteStudyArgumentsV2
{
    public int PartitionKey { get; }

    public IReadOnlyList<InstanceMetadata> InstanceMetadataList { get; }

    public string StudyInstanceUid { get; }

    public string ChangeDataset { get; set; }

    public CompleteStudyArgumentsV2(int partitionKey, string studyInstanceUid, string changeDataset, IReadOnlyList<InstanceMetadata> instanceMetadataList)
    {
        PartitionKey = partitionKey;
        StudyInstanceUid = EnsureArg.IsNotEmptyOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        ChangeDataset = EnsureArg.IsNotNull(changeDataset, nameof(changeDataset));
        InstanceMetadataList = EnsureArg.IsNotNull(instanceMetadataList, nameof(instanceMetadataList));
    }
}
