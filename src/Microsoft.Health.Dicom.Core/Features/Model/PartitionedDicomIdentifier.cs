// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class PartitionedDicomIdentifier : DicomIdentifier
{
    public int PartitionKey { get; }
    public PartitionedDicomIdentifier(DicomIdentifier dicomIdentifier, int partitionKey = DefaultPartition.Key) :
        base(EnsureArg.IsNotNull(dicomIdentifier, nameof(dicomIdentifier)).StudyInstanceUid,
        dicomIdentifier.SeriesInstanceUid,
        dicomIdentifier.SopInstanceUid)
    {
        PartitionKey = partitionKey;
    }
}
