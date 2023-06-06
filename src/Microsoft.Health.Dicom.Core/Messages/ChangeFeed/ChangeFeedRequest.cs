// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

public class ChangeFeedRequest : IRequest<ChangeFeedResponse>
{
    public ChangeFeedRequest(TimeRange range, long offset, int limit, ChangeFeedOrder order, bool includeMetadata)
    {
        Range = range;
        Offset = offset;
        Limit = limit;
        Order = order;
        IncludeMetadata = includeMetadata;
    }

    public TimeRange Range { get; }

    public int Limit { get; }

    public long Offset { get; }

    public ChangeFeedOrder Order { get; }

    public bool IncludeMetadata { get; }
}
