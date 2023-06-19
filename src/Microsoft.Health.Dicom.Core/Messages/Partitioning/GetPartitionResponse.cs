// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


namespace Microsoft.Health.Dicom.Core.Messages.Partitioning;

public class GetPartitionResponse
{
    public GetPartitionResponse(Features.Partitioning.Partition partition)
    {
        Partition = partition;
    }

    public Features.Partitioning.Partition Partition { get; }
}
