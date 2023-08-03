// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Core.Messages.Partitioning;

public class GetPartitionsResponse
{
    public GetPartitionsResponse(IReadOnlyCollection<Partition> entries)
    {
        EnsureArg.IsNotNull(entries, nameof(entries));

        Entries = entries;
    }

    public IReadOnlyCollection<Partition> Entries { get; }
}
