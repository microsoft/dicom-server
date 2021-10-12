// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Partition
{
    public class PartitionEntry
    {
        public string PartitionName { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public PartitionEntry(string partitionId, DateTimeOffset createdDate)
        {
            PartitionName = EnsureArg.IsNotNull(partitionId, nameof(partitionId));
            CreatedDate = createdDate;
        }
    }
}
