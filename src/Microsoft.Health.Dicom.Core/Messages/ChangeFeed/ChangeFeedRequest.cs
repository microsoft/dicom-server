// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

public class ChangeFeedRequest : IRequest<ChangeFeedResponse>
{
    public ChangeFeedRequest(TimeRange range, long offset, int limit, bool includeMetadata, ChangeFeedOrder order)
    {
        Range = range;
        Offset = offset;
        Limit = limit;
        IncludeMetadata = includeMetadata;
        Order = order;
    }

    public TimeRange Range { get; }

    public int Limit { get; }

    public long Offset { get; }

    public bool IncludeMetadata { get; }

    public ChangeFeedOrder Order { get; }
}
