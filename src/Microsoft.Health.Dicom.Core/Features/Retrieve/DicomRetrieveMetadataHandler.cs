// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    internal class DicomRetrieveMetadataHandler : IRequestHandler<DicomRetrieveMetadataRequest, DicomRetrieveMetadataResponse>
    {
        private readonly IDicomRetrieveMetadataService _dicomRetrieveMetadataService;

        public DicomRetrieveMetadataHandler(IDicomRetrieveMetadataService dicomRetrieveMetadataService)
        {
            EnsureArg.IsNotNull(dicomRetrieveMetadataService, nameof(dicomRetrieveMetadataService));
            _dicomRetrieveMetadataService = dicomRetrieveMetadataService;
        }

        public async Task<DicomRetrieveMetadataResponse> Handle(DicomRetrieveMetadataRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ValidateRetrieveMetadataRequest(request);

            return await _dicomRetrieveMetadataService.GetDicomInstanceMetadataAsync(
                request,
                cancellationToken);
        }

        private void ValidateRetrieveMetadataRequest(DicomRetrieveMetadataRequest request)
        {
            ResourceType inputResourceType = request.ResourceType;

            switch (inputResourceType)
            {
                case ResourceType.Study:
                    DicomIdentifierValidator.ValidateAndThrow(request.StudyInstanceUid, nameof(request.StudyInstanceUid));
                    break;
                case ResourceType.Series:
                    DicomIdentifierValidator.ValidateAndThrow(request.StudyInstanceUid, nameof(request.StudyInstanceUid));
                    DicomIdentifierValidator.ValidateAndThrow(request.SeriesInstanceUid, nameof(request.SeriesInstanceUid));
                    break;
                case ResourceType.Instance:
                    DicomIdentifierValidator.ValidateAndThrow(request.StudyInstanceUid, nameof(request.StudyInstanceUid));
                    DicomIdentifierValidator.ValidateAndThrow(request.SeriesInstanceUid, nameof(request.SeriesInstanceUid));
                    DicomIdentifierValidator.ValidateAndThrow(request.SopInstanceUid, nameof(request.SopInstanceUid));
                    break;
            }
        }
    }
}
