// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed;

public class ChangeFeedLatestRequest : IRequest<ChangeFeedLatestResponse>
{
    public ChangeFeedLatestRequest(bool includeMetadata, ChangeFeedOrder order)
    {
        IncludeMetadata = includeMetadata;
        Order = order;
    }

    public bool IncludeMetadata { get; }

    public ChangeFeedOrder Order { get; }
}
