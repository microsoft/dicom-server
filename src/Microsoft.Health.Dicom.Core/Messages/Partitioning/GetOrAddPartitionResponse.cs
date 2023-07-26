// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Core.Messages.Partitioning;

public class GetOrAddPartitionResponse
{
    public GetOrAddPartitionResponse(Partition partition)
    {
        Partition = partition;
    }

    public Partition Partition { get; }
}
