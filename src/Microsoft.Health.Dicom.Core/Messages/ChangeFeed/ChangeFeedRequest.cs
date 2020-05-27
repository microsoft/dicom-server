// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed
{
    public class ChangeFeedRequest : IRequest<ChangeFeedResponse>
    {
        public ChangeFeedRequest(int offset, int limit, bool includeMetadata)
        {
            Offset = offset;
            Limit = limit;
            IncludeMetadata = includeMetadata;
        }

        public int Limit { get; }

        public int Offset { get; }

        public bool IncludeMetadata { get; }
    }
}
