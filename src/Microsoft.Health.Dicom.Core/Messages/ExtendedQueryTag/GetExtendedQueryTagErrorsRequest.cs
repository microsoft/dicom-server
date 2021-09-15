// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsRequest : IRequest<GetExtendedQueryTagErrorsResponse>
    {
        public GetExtendedQueryTagErrorsRequest(string path, int limit, int offset)
        {
            EnsureArg.IsNotNullOrWhiteSpace(path, nameof(path));
            if (limit < 1 || limit > 200)
            {
                throw new BadRequestException(string.Format(DicomCoreResource.PaginationLimitOutOfRange, limit, 1, 200));
            }

            if (offset < 0)
            {
                throw new BadRequestException(string.Format(DicomCoreResource.PaginationNegativeOffset, offset));
            }

            Path = path;
            Limit = limit;
            Offset = offset;
        }

        public string Path { get; }

        public int Limit { get; }

        public int Offset { get; }
    }
}
