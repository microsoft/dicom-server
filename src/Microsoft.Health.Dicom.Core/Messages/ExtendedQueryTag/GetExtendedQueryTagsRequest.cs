// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagsRequest : IRequest<GetExtendedQueryTagsResponse>
    {
        public GetExtendedQueryTagsRequest(int limit, int offset)
        {
            Limit = EnsureArg.IsInRange(limit, 1, 200, nameof(limit));
            Offset = EnsureArg.IsGte(offset, 0, nameof(offset));
        }

        public int Limit { get; }

        public int Offset { get; }
    }
}
