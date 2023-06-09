// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Update;

public class UpdateInstanceOperationService : IUpdateInstanceOperationService
{
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IDicomRequestContextAccessor _contextAccessor;
    private readonly ILogger<UpdateInstanceOperationService> _logger;

    private static readonly OperationQueryCondition<DicomOperation> Query = new OperationQueryCondition<DicomOperation>
    {
        Operations = new DicomOperation[] { DicomOperation.Update },
        Statuses = new OperationStatus[]
        {
            OperationStatus.NotStarted,
            OperationStatus.Running,
        }
    };

    public UpdateInstanceOperationService(
        IGuidFactory guidFactory,
        IDicomOperationsClient client,
        IDicomRequestContextAccessor contextAccessor,
        ILogger<UpdateInstanceOperationService> logger)
    {
        EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _guidFactory = guidFactory;
        _client = client;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task<UpdateInstanceResponse> QueueUpdateOperationAsync(
        UpdateSpecification updateSpecification,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(updateSpecification, nameof(updateSpecification));
        EnsureArg.IsNotNull(updateSpecification.ChangeDataset, nameof(updateSpecification.ChangeDataset));

        UpdateRequestValidator.ValidateRequest(updateSpecification);
        DicomDataset failedSop = UpdateRequestValidator.ValidateDicomDataset(updateSpecification.ChangeDataset);

        if (failedSop.Any())
        {
            return new UpdateInstanceResponse(failedSop);
        }

        OperationReference activeOperation = await _client
            .FindOperationsAsync(Query, cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeOperation != null)
            throw new ExistingOperationException(activeOperation, "update");

        Guid operationId = _guidFactory.Create();

        EnsureArg.IsNotNull(updateSpecification, nameof(updateSpecification));
        EnsureArg.IsNotNull(updateSpecification.ChangeDataset, nameof(updateSpecification.ChangeDataset));

        int partitionKey = _contextAccessor.RequestContext.GetPartitionKey();

        try
        {
            var operation = await _client.StartUpdateOperationAsync(operationId, updateSpecification, partitionKey, cancellationToken);
            return new UpdateInstanceResponse(operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start update operation");
            throw;
        }
    }
}
