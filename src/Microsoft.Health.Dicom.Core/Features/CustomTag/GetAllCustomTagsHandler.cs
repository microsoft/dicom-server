// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class GetAllCustomTagsHandler : IRequestHandler<GetAllCustomTagsRequest, GetAllCustomTagsResponse>
    {
        private readonly IGetCustomTagsService _getCustomTagsService;

        public GetAllCustomTagsHandler(IGetCustomTagsService getCustomTagsService)
        {
            EnsureArg.IsNotNull(getCustomTagsService, nameof(getCustomTagsService));
            _getCustomTagsService = getCustomTagsService;
        }

        public async Task<GetAllCustomTagsResponse> Handle(GetAllCustomTagsRequest request, CancellationToken cancellationToken)
        {
            return await _getCustomTagsService.GetAllCustomTagsAsync(cancellationToken);
        }
    }
}
