// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveDicomResourceHandler : IRequestHandler<RetrieveDicomResourceRequest, RetrieveDicomResourceResponse>
    {
        private readonly IDicomRetrieveResourceService _dicomRetrieveResourceService;

        public RetrieveDicomResourceHandler(IDicomRetrieveResourceService dicomRetrieveResourceService)
        {
            EnsureArg.IsNotNull(dicomRetrieveResourceService, nameof(dicomRetrieveResourceService));
            _dicomRetrieveResourceService = dicomRetrieveResourceService;
        }

        public async Task<RetrieveDicomResourceResponse> Handle(
            RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            return await _dicomRetrieveResourceService.GetInstanceResourceAsync(message, cancellationToken);
        }
    }
}
