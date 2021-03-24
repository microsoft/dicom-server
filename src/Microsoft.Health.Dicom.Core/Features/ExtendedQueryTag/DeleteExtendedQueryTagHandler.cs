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
    public class DeleteExtendedQueryTagHandler : IRequestHandler<DeleteExtendedQueryTagRequest, DeleteExtendedQueryTagResponse>
    {
        private readonly IDeleteExtendedQueryTagService _deleteExtendedQueryTagService;

        public DeleteExtendedQueryTagHandler(IDeleteExtendedQueryTagService deleteExtendedQueryTagService)
        {
            EnsureArg.IsNotNull(deleteExtendedQueryTagService, nameof(deleteExtendedQueryTagService));
            _deleteExtendedQueryTagService = deleteExtendedQueryTagService;
        }

        public async Task<DeleteExtendedQueryTagResponse> Handle(DeleteExtendedQueryTagRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            await _deleteExtendedQueryTagService.DeleteExtendedQueryTagAsync(request.TagPath, cancellationToken);
            return new DeleteExtendedQueryTagResponse();
        }
    }
}
