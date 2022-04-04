// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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

    public override string ToString()
    {
        return PartitionKey + "/" + base.ToString();
    }

    public static new PartitionedDicomIdentifier Parse(string input)
    {
        EnsureArg.IsNotNull(input, nameof(input));
        int iSplitter = input.IndexOf('/', StringComparison.OrdinalIgnoreCase);
        if (iSplitter <= 0)
        {
            throw new FormatException();
        }
        string partitionText = input.Substring(0, iSplitter);
        int partitionKey = int.Parse(partitionText);
        DicomIdentifier identifier = DicomIdentifier.Parse(input.Substring(iSplitter + 1));
        return new PartitionedDicomIdentifier(identifier, partitionKey);
    }
}
