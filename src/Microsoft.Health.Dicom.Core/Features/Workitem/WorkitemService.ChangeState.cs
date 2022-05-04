// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
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
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        try
        {
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

            ValidateChangeWorkitemStateRequest(workitemMetadata);

            await _workitemOrchestrator.UpdateWorkitemStateAsync(
                    dataset, // prepare the final dataset from the (incoming dataset + blob store dataset)
                    workitemMetadata,
                    dataset.GetProcedureState(),
                    cancellationToken)
                .ConfigureAwait(false);

            _responseBuilder.AddSuccess(string.Format(
                DicomCoreResource.WorkitemChangeStateRequestSuccess,
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepStateStringValue));
        }
        catch (Exception ex)
        {
            ushort? failureCode = FailureReasonCodes.ProcessingFailure;

            switch (ex)
            {
                case DatasetValidationException dvEx:
                    failureCode = dvEx.FailureCode;
                    break;

                case DicomValidationException _:
                case ValidationException _:
                case WorkitemUpdateNotAllowedException:
                    failureCode = FailureReasonCodes.UpsInstanceUpdateNotAllowed;
                    break;

                case WorkitemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;

                case WorkitemIsInFinalStateException wiFex:
                    failureCode = wiFex.FailureCode;
                    break;
            }

            _logger.LogInformation(ex,
                "Validation failed for the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

            _responseBuilder.AddFailure(failureCode, ex.Message, dataset);
        }

        return _responseBuilder.BuildChangeWorkitemStateResponse();
    }

    private static void ValidateChangeWorkitemStateRequest(WorkitemMetadataStoreEntry workitemMetadata)
    {
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        if (workitemMetadata.ProcedureStepState == ProcedureStepState.Canceled ||
            workitemMetadata.ProcedureStepState == ProcedureStepState.Completed)
        {
            throw new WorkitemIsInFinalStateException(
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepState);
        }

        if (workitemMetadata.ProcedureStepState == ProcedureStepState.InProgress)
        {
            throw new WorkitemUpdateNotAllowedException(
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepStateStringValue);
        }
    }
}
