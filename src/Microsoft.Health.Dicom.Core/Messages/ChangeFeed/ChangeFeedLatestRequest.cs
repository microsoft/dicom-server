// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed
{
    public class ChangeFeedLatestRequest : IRequest<ChangeFeedLatestResponse>
    {
        public ChangeFeedLatestRequest(bool includeMetadata)
        {
            IncludeMetadata = includeMetadata;
        }

        public bool IncludeMetadata { get; }
    }
}
