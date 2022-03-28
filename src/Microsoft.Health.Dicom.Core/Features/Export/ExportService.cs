// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

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
    /// Add Extended Query Tags.
    /// </summary>
    /// <param name="exportInput">The export input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async Task<ExportResponse> ExportAsync(ExportInput exportInput, CancellationToken cancellationToken)
    {
        Guid operationId = await _client.StartExportAsync(GetOperationInput(exportInput), cancellationToken);
        return new ExportResponse(new OperationReference(operationId, _uriResolver.ResolveOperationStatusUri(operationId)));
    }

    private static ExportOperationInput GetOperationInput(ExportInput input)
    {
        // TODO: inmplement this method
        HashSet<string> studies = new HashSet<string>();
        Dictionary<string, HashSet<string>> series = new Dictionary<string, HashSet<string>>();
        Dictionary<string, Dictionary<string, HashSet<string>>> instances = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        foreach (string id in input.Source.IdFilter.Ids)
        {

        }
    }
}
