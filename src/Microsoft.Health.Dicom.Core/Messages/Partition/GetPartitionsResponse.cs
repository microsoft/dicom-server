// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Messages.Partition
{
    public class GetPartitionsResponse
    {
        public GetPartitionsResponse(IReadOnlyCollection<PartitionEntry> entries)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));

            Entries = entries;
        }

        public IReadOnlyCollection<PartitionEntry> Entries { get; }
    }
}
