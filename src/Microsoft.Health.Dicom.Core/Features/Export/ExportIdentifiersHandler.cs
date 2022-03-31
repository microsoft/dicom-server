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
using Microsoft.Health.Dicom.Core.Messages.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public class ExportIdentifiersHandler : BaseHandler, IRequestHandler<ExportIdentifiersRequest, ExportIdentifiersResponse>
{
    private readonly IExportService _exportService;

    public ExportIdentifiersHandler(IAuthorizationService<DataActions> authorizationService, IExportService exportService)
        : base(authorizationService)
        => _exportService = EnsureArg.IsNotNull(exportService, nameof(exportService));

    public async Task<ExportIdentifiersResponse> Handle(ExportIdentifiersRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (await AuthorizationService.CheckAccess(DataActions.Export, cancellationToken) != DataActions.Export)
        {
            throw new UnauthorizedDicomActionException(DataActions.Export);
        }

        return await _exportService.StartExportingIdentifiersAsync(request.Input, cancellationToken);
    }
}
