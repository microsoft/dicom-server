// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Store;

public class UpdateInstanceService : IUpdateInstanceService
{
    private readonly IGuidFactory _guidFactory;
    private readonly IDicomOperationsClient _client;
    private readonly IUrlResolver _urlResolver;

    private static readonly OperationQueryCondition<DicomOperation> Query = new OperationQueryCondition<DicomOperation>
    {
        Operations = new DicomOperation[] { DicomOperation.Update },
        Statuses = new OperationStatus[]
        {
            OperationStatus.NotStarted,
            OperationStatus.Running,
        }
    };

    public UpdateInstanceService(
        IGuidFactory guidFactory,
        IDicomOperationsClient client,
        IUrlResolver iUrlResolver)
    {
        EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(iUrlResolver, nameof(iUrlResolver));

        _guidFactory = guidFactory;
        _client = client;
        _urlResolver = iUrlResolver;
    }

    public async Task<OperationReference> UpdateInstanceAsync(
        UpdateSpecification spec,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(spec, nameof(spec));

        OperationReference activeOperation = await _client
            .FindOperationsAsync(Query, cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeOperation != null)
            throw new ExistingUpdateOperationException(activeOperation);

        var operationId = _guidFactory.Create();
        OperationReference operation = new OperationReference(operationId, _urlResolver.ResolveOperationStatusUri(operationId));

        return operation;
    }
}
