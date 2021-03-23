// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Security.Authorization;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    internal class RetrieveMetadataHandler : BaseHandler, IRequestHandler<RetrieveMetadataRequest, RetrieveMetadataResponse>
    {
        private readonly IRetrieveMetadataService _retrieveMetadataService;

        public RetrieveMetadataHandler(IAuthorizationService<DataActions> authorizationService, IRetrieveMetadataService retrieveMetadataService)
            : base(authorizationService)
        {
            _retrieveMetadataService = EnsureArg.IsNotNull(retrieveMetadataService, nameof(retrieveMetadataService));
        }

        public async Task<RetrieveMetadataResponse> Handle(RetrieveMetadataRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException(DataActions.Read);
            }

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
