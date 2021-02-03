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
    public class DeleteCustomTagHandler : IRequestHandler<DeleteCustomTagRequest, DeleteCustomTagResponse>
    {
        private readonly ICustomTagService _customTagService;

        public DeleteCustomTagHandler(ICustomTagService customTagService)
        {
            EnsureArg.IsNotNull(customTagService, nameof(customTagService));
            _customTagService = customTagService;
        }

        public async Task<DeleteCustomTagResponse> Handle(DeleteCustomTagRequest request, CancellationToken cancellationToken)
        {
            await _customTagService.DeleteCustomTagAsync(request.TagPath, cancellationToken);
            return new DeleteCustomTagResponse();
        }
    }
}
