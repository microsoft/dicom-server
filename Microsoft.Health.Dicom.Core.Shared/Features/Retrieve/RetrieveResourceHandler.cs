// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveResourceHandler : BaseHandler, IRequestHandler<RetrieveResourceRequest, RetrieveResourceResponse>
    {
        private readonly IRetrieveResourceService _retrieveResourceService;

        public RetrieveResourceHandler(IAuthorizationService<DataActions> authorizationService, IRetrieveResourceService retrieveResourceService)
            : base(authorizationService)
        {
            _retrieveResourceService = EnsureArg.IsNotNull(retrieveResourceService, nameof(retrieveResourceService));
        }

        public async Task<RetrieveResourceResponse> Handle(
            RetrieveResourceRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException(DataActions.Read);
            }

            ValidateRetrieveResourceRequest(request);

            return await _retrieveResourceService.GetInstanceResourceAsync(request, cancellationToken);
        }

        private static void ValidateRetrieveResourceRequest(RetrieveResourceRequest request)
        {
            RetrieveRequestValidator.ValidateInstanceIdentifiers(request.ResourceType, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid);
            if (request.ResourceType == ResourceType.Frames)
            {
                RetrieveRequestValidator.ValidateFrames(request.Frames);
            }
        }
    }
}
