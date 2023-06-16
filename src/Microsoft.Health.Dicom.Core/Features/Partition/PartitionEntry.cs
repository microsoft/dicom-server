// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Partition;

public class PartitionEntry
{
    public int PartitionKey { get; set; }

    public string PartitionName { get; set; }

    public static PartitionEntry Default => new PartitionEntry(DefaultPartition.Key, DefaultPartition.Name);

    public PartitionEntry(int partitionKey, string partitionName)
    {
        PartitionKey = partitionKey;
        PartitionName = EnsureArg.IsNotNull(partitionName, nameof(partitionName));
    }
}
