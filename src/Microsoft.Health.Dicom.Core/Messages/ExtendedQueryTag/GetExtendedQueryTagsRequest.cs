// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagsRequest : IRequest<GetExtendedQueryTagsResponse>
    {
        public GetExtendedQueryTagsRequest(int limit, int offset)
        {
            if (limit < 1 || limit > 200)
            {
                throw new BadRequestException(string.Format(DicomCoreResource.PaginationLimitOutOfRange, limit, 1, 200));
            }

            if (offset < 0)
            {
                throw new BadRequestException(string.Format(DicomCoreResource.PaginationNegativeOffset, offset));
            }

            Limit = limit;
            Offset = offset;
        }

        public int Limit { get; }

        public int Offset { get; }
    }
}
