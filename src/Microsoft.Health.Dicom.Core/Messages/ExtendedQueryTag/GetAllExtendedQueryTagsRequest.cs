// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetAllExtendedQueryTagsRequest : IRequest<GetAllExtendedQueryTagsResponse>
    {
        public GetAllExtendedQueryTagsRequest()
        {
        }
    }
}
