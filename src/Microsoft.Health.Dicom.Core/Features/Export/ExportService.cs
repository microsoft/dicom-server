// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class ExportService : IExportService
{
    private readonly ExportSourceFactory _sourceFactory;
    private readonly ExportSinkFactory _sinkFactory;
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;

    public ExportService(
        ExportSourceFactory sourceFactory,
        ExportSinkFactory sinkFactory,
        IGuidFactory guidFactory,
        IDicomOperationsClient client)
    {
        _sourceFactory = EnsureArg.IsNotNull(sourceFactory, nameof(sourceFactory));
        _sinkFactory = EnsureArg.IsNotNull(sinkFactory, nameof(sinkFactory));
        _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        _client = EnsureArg.IsNotNull(client, nameof(client));
    }

    public async Task<OperationReference> StartExportAsync(ExportSpecification specification, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(specification, nameof(specification));

        Guid operationId = _guidFactory.Create();

        // Validate the input and update the specification
        specification.Source = await _sourceFactory.ValidateAsync(specification.Source, cancellationToken);
        specification.Destination = await _sinkFactory.ValidateAsync(specification.Destination, operationId, cancellationToken);

        // Start the operation
        return await _client.StartExportAsync(operationId, specification, cancellationToken);
    }
}
