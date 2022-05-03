// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to process the list of <see cref="IDicomInstanceEntry"/>.
/// </summary>
public partial class WorkitemService
{
    public async Task<ChangeWorkitemStateResponse> ProcessChangeStateAsync(
        DicomDataset dataset,
        string workitemInstanceUid,
        string workitemCurrentState,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        var workitemMetadata = await _workitemOrchestrator
            .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
            .ConfigureAwait(false);

        if (workitemMetadata == null)
        {
            _responseBuilder.AddFailure(
                FailureReasonCodes.UpsInstanceNotFound,
                string.Format(DicomCoreResource.WorkitemInstanceNotFound, workitemInstanceUid),
                dataset);
            return _responseBuilder.BuildChangeWorkitemStateResponse();
        }

        ValidateChangeWorkitemStateRequest(workitemMetadata, workitemCurrentState);

        await _workitemOrchestrator.UpdateWorkitemStateAsync(
                dataset, // prepare the final dataset from the (incoming dataset + blob store dataset)
                workitemMetadata,
                ProcedureStepState.None, // read from the incoming dataset
                cancellationToken)
            .ConfigureAwait(false);

        return _responseBuilder.BuildChangeWorkitemStateResponse();
    }

    private static void ValidateChangeWorkitemStateRequest(WorkitemMetadataStoreEntry workitemMetadata, string expectedProcedureStepState)
    {
        var currentState = ProcedureStepStateExtensions.GetProcedureStepState(expectedProcedureStepState);

        if (currentState != workitemMetadata.ProcedureStepState)
        {
            throw new BadRequestException(DicomCoreResource.WorkitemUpdateIsNotAllowed);
        }
    }
}
