// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Functions.Update.Models;

public class UpdateInstanceWatermarkArgumentsV2
{
    public Partition Partition { get; }

    public string StudyInstanceUid { get; }

    public UpdateInstanceWatermarkArgumentsV2(Partition partition, string studyInstanceUid)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        StudyInstanceUid = EnsureArg.IsNotEmptyOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
    }
}
