// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Delete;

namespace Microsoft.Health.Dicom.Core.Features.Delete
{
    public class DicomDeleteHandler : IRequestHandler<DicomDeleteResourcesRequest, DicomDeleteResourcesResponse>
    {
        private readonly IDicomDeleteService _dicomDeleteService;

        public DicomDeleteHandler(IDicomDeleteService dicomDeleteService)
        {
            EnsureArg.IsNotNull(dicomDeleteService, nameof(dicomDeleteService));

            _dicomDeleteService = dicomDeleteService;
        }

        /// <inheritdoc />
        public async Task<DicomDeleteResourcesResponse> Handle(DicomDeleteResourcesRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ValidateDeleteResourcesRequest(request);

            switch (request.ResourceType)
            {
                case ResourceType.Study:
                    await _dicomDeleteService.DeleteStudyAsync(request.StudyInstanceUid, cancellationToken);
                    break;
                case ResourceType.Series:
                    await _dicomDeleteService.DeleteSeriesAsync(request.StudyInstanceUid, request.SeriesInstanceUid, cancellationToken);
                    break;
                case ResourceType.Instance:
                    await _dicomDeleteService.DeleteInstanceAsync(request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid, cancellationToken);
                    break;
                default:
                    Debug.Fail($"Unknown delete transaction type: {request.ResourceType}", nameof(request));
                    break;
            }

            return new DicomDeleteResourcesResponse();
        }

        private void ValidateDeleteResourcesRequest(DicomDeleteResourcesRequest request)
        {
            DicomIdentifierValidator.ValidateAndThrow(request.StudyInstanceUid, nameof(request.StudyInstanceUid));

            switch (request.ResourceType)
            {
                case ResourceType.Series:
                    DicomIdentifierValidator.ValidateAndThrow(request.SeriesInstanceUid, nameof(request.SeriesInstanceUid));
                    break;
                case ResourceType.Instance:
                    DicomIdentifierValidator.ValidateAndThrow(request.SeriesInstanceUid, nameof(request.SeriesInstanceUid));
                    DicomIdentifierValidator.ValidateAndThrow(request.SopInstanceUid, nameof(request.SopInstanceUid));
                    break;
            }
        }
    }
}
