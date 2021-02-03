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
    public class AddCustomTagHandler : IRequestHandler<AddCustomTagRequest, AddCustomTagResponse>
    {
        private readonly IAddCustomTagService _addCustomTagService;

        public AddCustomTagHandler(IAddCustomTagService addCustomTagService)
        {
            EnsureArg.IsNotNull(addCustomTagService, nameof(addCustomTagService));
            _addCustomTagService = addCustomTagService;
        }

        public async Task<AddCustomTagResponse> Handle(AddCustomTagRequest request, CancellationToken cancellationToken)
        {
            return await _addCustomTagService.AddCustomTagAsync(request.CustomTags, cancellationToken);
        }
    }
}
