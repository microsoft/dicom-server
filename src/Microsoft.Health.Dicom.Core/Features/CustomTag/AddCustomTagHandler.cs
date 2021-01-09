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
        private readonly ICustomTagService _customTagService;

        public AddCustomTagHandler(ICustomTagService customTagService)
        {
            EnsureArg.IsNotNull(customTagService, nameof(customTagService));
            _customTagService = customTagService;
        }

        public async Task<AddCustomTagResponse> Handle(AddCustomTagRequest request, CancellationToken cancellationToken)
        {
            return await _customTagService.AddCustomTagAsync(request.CustomTags, cancellationToken);
        }
    }
}
