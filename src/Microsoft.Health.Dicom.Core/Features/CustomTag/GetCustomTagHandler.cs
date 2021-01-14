// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class GetCustomTagHandler : IRequestHandler<GetCustomTagRequest, GetCustomTagResponse>
    {
        private readonly IGetCustomTagsService _getCustomTagsService;

        public GetCustomTagHandler(IGetCustomTagsService getCustomTagsService)
        {
            EnsureArg.IsNotNull(getCustomTagsService, nameof(getCustomTagsService));
            _getCustomTagsService = getCustomTagsService;
        }

        public async Task<GetCustomTagResponse> Handle(GetCustomTagRequest request, CancellationToken cancellationToken)
        {
            TagPathValidator.Validate(request.CustomTagPath);

            return await _getCustomTagsService.GetCustomTagAsync(request.CustomTagPath, cancellationToken);
        }
    }
}
