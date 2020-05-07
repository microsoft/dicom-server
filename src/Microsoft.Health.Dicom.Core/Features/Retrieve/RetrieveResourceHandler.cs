// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveResourceHandler : IRequestHandler<RetrieveResourceRequest, RetrieveResourceResponse>
    {
        private readonly IRetrieveResourceService _retrieveResourceService;

        public RetrieveResourceHandler(IRetrieveResourceService retrieveResourceService)
        {
            EnsureArg.IsNotNull(retrieveResourceService, nameof(retrieveResourceService));
            _retrieveResourceService = retrieveResourceService;
        }

        public async Task<RetrieveResourceResponse> Handle(
            RetrieveResourceRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ValidateRetrieveResourceRequest(request);

            return await _retrieveResourceService.GetInstanceResourceAsync(request, cancellationToken);
        }

        private void ValidateRetrieveResourceRequest(RetrieveResourceRequest request)
        {
            RetrieveRequestValidator.ValidateInstanceIdentifiers(request.ResourceType, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid);
            RetrieveRequestValidator.ValidateTransferSyntax(request.RequestedRepresentation, request.OriginalTransferSyntaxRequested());

            if (request.ResourceType == ResourceType.Frames)
            {
                RetrieveRequestValidator.ValidateFrames(request.Frames);
            }
        }
    }
}
