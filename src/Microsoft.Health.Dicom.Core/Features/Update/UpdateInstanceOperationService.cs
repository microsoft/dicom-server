// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Update;

public class UpdateInstanceOperationService : IUpdateInstanceOperationService
{
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IUrlResolver _urlResolver;
    private readonly IDicomRequestContextAccessor _contextAccessor;

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
        IUrlResolver iUrlResolver,
        IDicomRequestContextAccessor contextAccessor)
    {
        EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(iUrlResolver, nameof(iUrlResolver));
        EnsureArg.IsNotNull(contextAccessor, nameof(contextAccessor));

        _guidFactory = guidFactory;
        _client = client;
        _urlResolver = iUrlResolver;
        _contextAccessor = contextAccessor;
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
            throw new ExistingUpdateOperationException(activeOperation);

        var operationId = _guidFactory.Create();
        var operation = new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));
        return new UpdateInstanceResponse(operation);
    }
}
