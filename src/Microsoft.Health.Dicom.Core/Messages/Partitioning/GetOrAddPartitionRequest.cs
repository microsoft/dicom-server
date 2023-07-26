// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Partitioning;

public class GetOrAddPartitionRequest : IRequest<GetOrAddPartitionResponse>
{
    public GetOrAddPartitionRequest(string partitionName, bool addIfNotExists)
    {
        PartitionName = partitionName;
        AddIfNotExists = addIfNotExists;
    }

    /// <summary>
    /// Should the request attempt add if the partition doesn't exist.
    /// </summary>
    public bool AddIfNotExists { get; set; }

    /// <summary>
    /// Data Partition name
    /// </summary>
    public string PartitionName { get; }
}
