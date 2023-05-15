// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

public class ChangeFeedLatestRequest : IRequest<ChangeFeedLatestResponse>
{
    public ChangeFeedLatestRequest(ChangeFeedOrder order, bool includeMetadata)
    {
        Order = order;
        IncludeMetadata = includeMetadata;
    }

    public ChangeFeedOrder Order { get; }

    public bool IncludeMetadata { get; }
}
