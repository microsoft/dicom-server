// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Client.Models;

public class Partition
{
    public int PartitionKey { get; set; }

    public string PartitionName { get; set; }

    public static Partition Default => new Partition(DefaultPartition.Key, DefaultPartition.Name);

    public Partition(int partitionKey, string partitionName)
    {
        PartitionKey = partitionKey;
        PartitionName = EnsureArg.IsNotNull(partitionName, nameof(partitionName));
    }
}