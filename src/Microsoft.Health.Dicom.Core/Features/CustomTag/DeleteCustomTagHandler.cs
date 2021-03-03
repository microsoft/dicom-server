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
        private readonly IDeleteCustomTagService _deleteCustomTagService;

        public DeleteCustomTagHandler(IDeleteCustomTagService deleteCustomTagService)
        {
            EnsureArg.IsNotNull(deleteCustomTagService, nameof(deleteCustomTagService));
            _deleteCustomTagService = deleteCustomTagService;
        }

        public async Task<DeleteCustomTagResponse> Handle(DeleteCustomTagRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            await _deleteCustomTagService.DeleteCustomTagAsync(request.TagPath, cancellationToken);
            return new DeleteCustomTagResponse();
        }
    }
}
