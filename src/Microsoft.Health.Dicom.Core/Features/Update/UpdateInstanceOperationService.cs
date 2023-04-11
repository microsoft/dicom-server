// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Core.Features.Update;
public class UpdateInstanceOperationService : IUpdateInstanceOperationService
{
    private readonly ILogger<UpdateInstanceOperationService> _logger;
    private readonly IDicomOperationsClient _client;
    private readonly IGuidFactory _guidFactory;

    public UpdateInstanceOperationService(
        ILogger<UpdateInstanceOperationService> logger,
        IDicomOperationsClient client,
        IGuidFactory guidFactory)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
    }

    public async Task QueueUpdateOperationAsync(
        UpdateSpecification updateSpecification,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(updateSpecification, nameof(updateSpecification));
        EnsureArg.IsNotNull(updateSpecification.ChangeDataset, nameof(updateSpecification.ChangeDataset));

        var operationId = _guidFactory.Create();
        var partitionKey = 1;

        try
        {
            await _client.StartUpdateOperationAsync(operationId, updateSpecification, partitionKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start update operation");
            throw;
        }
    }
}
