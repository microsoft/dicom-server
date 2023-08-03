// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Partitioning;

public class GetPartitionRequest : IRequest<GetPartitionResponse>
{
    public GetPartitionRequest(string partitionName)
    {
        PartitionName = partitionName;
    }

    /// <summary>
    /// Data Partition name
    /// </summary>
    public string PartitionName { get; }
}
