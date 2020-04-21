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
    public class DicomRetrieveResourceHandler : IRequestHandler<DicomRetrieveResourceRequest, DicomRetrieveResourceResponse>
    {
        private readonly IDicomRetrieveResourceService _dicomRetrieveResourceService;

        public DicomRetrieveResourceHandler(IDicomRetrieveResourceService dicomRetrieveResourceService)
        {
            EnsureArg.IsNotNull(dicomRetrieveResourceService, nameof(dicomRetrieveResourceService));
            _dicomRetrieveResourceService = dicomRetrieveResourceService;
        }

        public async Task<DicomRetrieveResourceResponse> Handle(
            DicomRetrieveResourceRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ValidateRetrieveResourceRequest(request);

            return await _dicomRetrieveResourceService.GetInstanceResourceAsync(request, cancellationToken);
        }

        private void ValidateRetrieveResourceRequest(DicomRetrieveResourceRequest request)
        {
            DicomRetrieveRequestValidator.Validate(request.ResourceType, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid, request.Frames, request.RequestedRepresentation, request.OriginalTransferSyntaxRequested());
        }
    }
}
