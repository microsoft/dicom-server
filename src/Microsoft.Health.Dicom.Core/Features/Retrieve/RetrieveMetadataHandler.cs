// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    internal class RetrieveMetadataHandler : IRequestHandler<RetrieveMetadataRequest, RetrieveMetadataResponse>
    {
        private readonly IRetrieveMetadataService _retrieveMetadataService;

        public RetrieveMetadataHandler(IRetrieveMetadataService retrieveMetadataService)
        {
            EnsureArg.IsNotNull(retrieveMetadataService, nameof(retrieveMetadataService));
            _retrieveMetadataService = retrieveMetadataService;
        }

        public async Task<RetrieveMetadataResponse> Handle(RetrieveMetadataRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            ValidateRetrieveMetadataRequest(request);

            RetrieveMetadataResponse metadataResponse = null;

            switch (request.ResourceType)
            {
                case ResourceType.Study:
                    metadataResponse = await _retrieveMetadataService.RetrieveStudyInstanceMetadataAsync(request.StudyInstanceUid, request.IfNoneMatch, cancellationToken);
                    break;
                case ResourceType.Series:
                    metadataResponse = await _retrieveMetadataService.RetrieveSeriesInstanceMetadataAsync(request.StudyInstanceUid, request.SeriesInstanceUid, request.IfNoneMatch, cancellationToken);
                    break;
                case ResourceType.Instance:
                    metadataResponse = await _retrieveMetadataService.RetrieveSopInstanceMetadataAsync(request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid, request.IfNoneMatch, cancellationToken);
                    break;
                default:
                    Debug.Fail($"Unknown retrieve metadata transaction type: {request.ResourceType}", nameof(request));
                    break;
            }

            return metadataResponse;
        }

        private static void ValidateRetrieveMetadataRequest(RetrieveMetadataRequest request)
        {
            RetrieveRequestValidator.ValidateInstanceIdentifiers(request.ResourceType, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid);
        }
    }
}
