// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Messages.Partition
{
    public class AddPartitionResponse
    {
        public AddPartitionResponse(PartitionEntry partitionEntry)
        {
            PartitionEntry = partitionEntry;
        }

        public PartitionEntry PartitionEntry { get; }
    }
}
