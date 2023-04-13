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

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;
internal class RetrieveRenderedHandler : BaseHandler, IRequestHandler<RetrieveRenderedRequest, RetrieveRenderedResponse>
{
    private readonly IRetrieveRenderedService _retrieveRenderedService;

    public RetrieveRenderedHandler(IAuthorizationService<DataActions> authorizationService, IRetrieveRenderedService retrieveRenderedService)
        : base(authorizationService)
    {
        _retrieveRenderedService = EnsureArg.IsNotNull(retrieveRenderedService, nameof(retrieveRenderedService));
    }

    public async Task<RetrieveRenderedResponse> Handle(RetrieveRenderedRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
        {
            throw new UnauthorizedDicomActionException(DataActions.Read);
        }

        ValidateRetrieveRenderedRequest(request);

        return await _retrieveRenderedService.RetrieveRenderedImageAsync(request, cancellationToken);
    }

    private static void ValidateRetrieveRenderedRequest(RetrieveRenderedRequest request)
    {
        RetrieveRequestValidator.ValidateInstanceIdentifiers(request.ResourceType, request.StudyInstanceUid, request.SeriesInstanceUid, request.SopInstanceUid);

        if (request.ResourceType == ResourceType.Frames)
        {
            RetrieveRequestValidator.ValidateFrames(new[] { request.FrameNumber });
        }
    }
}
