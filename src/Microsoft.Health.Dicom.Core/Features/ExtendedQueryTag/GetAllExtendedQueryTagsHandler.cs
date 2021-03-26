// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class GetAllExtendedQueryTagsHandler : IRequestHandler<GetAllExtendedQueryTagsRequest, GetAllExtendedQueryTagsResponse>
    {
        private readonly IGetExtendedQueryTagsService _getExtendedQueryTagsService;

        public GetAllExtendedQueryTagsHandler(IGetExtendedQueryTagsService getExtendedQueryTagsService)
        {
            EnsureArg.IsNotNull(getExtendedQueryTagsService, nameof(getExtendedQueryTagsService));
            _getExtendedQueryTagsService = getExtendedQueryTagsService;
        }

        public async Task<GetAllExtendedQueryTagsResponse> Handle(GetAllExtendedQueryTagsRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            return await _getExtendedQueryTagsService.GetAllExtendedQueryTagsAsync(cancellationToken);
        }
    }
}
