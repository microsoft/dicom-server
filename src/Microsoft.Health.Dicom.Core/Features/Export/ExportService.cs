// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Export;

public class ExportService : IExportService
{

    private readonly IDicomOperationsClient _client;
    private readonly IUrlResolver _uriResolver;

    public ExportService(IDicomOperationsClient client, IUrlResolver uriResolver)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _uriResolver = EnsureArg.IsNotNull(uriResolver, nameof(uriResolver));
    }

    /// <summary>
    /// Export.
    /// </summary>
    /// <param name="exportInput">The export input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async Task<ExportResponse> ExportAsync(ExportInput exportInput, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(exportInput, nameof(exportInput));
        // TODO: validate input
        Guid operationId = await _client.StartExportAsync(exportInput, cancellationToken);
        return new ExportResponse(new OperationReference(operationId, _uriResolver.ResolveOperationStatusUri(operationId)));
    }
}
