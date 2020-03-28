// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class RetrieveDicomResourceHandler : IRequestHandler<RetrieveDicomResourceRequest, RetrieveDicomResourceResponse>
    {
        private readonly IDicomResourceRetrieveService _dicomResourceRetrieveService;

        public RetrieveDicomResourceHandler(IDicomResourceRetrieveService dicomResourceRetrieveService)
        {
            EnsureArg.IsNotNull(dicomResourceRetrieveService, nameof(dicomResourceRetrieveService));
            _dicomResourceRetrieveService = dicomResourceRetrieveService;
        }

        public async Task<RetrieveDicomResourceResponse> Handle(
            RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            return await _dicomResourceRetrieveService.GetInstanceResource(message, cancellationToken);
        }
    }
}
