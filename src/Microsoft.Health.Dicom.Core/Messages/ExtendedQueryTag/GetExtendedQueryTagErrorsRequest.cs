// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsRequest : IRequest<GetExtendedQueryTagErrorsResponse>
    {
        public GetExtendedQueryTagErrorsRequest(string path, int limit, int offset)
        {
            Path = EnsureArg.IsNotNullOrWhiteSpace(path, nameof(path));
            Limit = EnsureArg.IsInRange(limit, 1, 200, nameof(limit));
            Offset = EnsureArg.IsGte(offset, 0, nameof(offset));
        }

        public string Path { get; }

        public int Limit { get; }

        public int Offset { get; }
    }
}
