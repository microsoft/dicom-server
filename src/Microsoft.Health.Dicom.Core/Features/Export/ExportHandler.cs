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

public class ExportHandler : BaseHandler, IRequestHandler<ExportRequest, ExportResponse>
{
    private readonly IExportService _exportService;
    public ExportHandler(IAuthorizationService<DataActions> authorizationService, IExportService exportService)
        : base(authorizationService)
    {
        EnsureArg.IsNotNull(exportService, nameof(exportService));
        _exportService = exportService;
    }

    public async Task<ExportResponse> Handle(ExportRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (await AuthorizationService.CheckAccess(DataActions.Export, cancellationToken) != DataActions.Export)
        {
            throw new UnauthorizedDicomActionException(DataActions.Export);
        }

        return await _exportService.ExportAsync(request.ExportInput, cancellationToken);
    }
}
