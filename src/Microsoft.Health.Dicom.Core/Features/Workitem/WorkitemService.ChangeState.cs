// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
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
    // The legal values correspond to the requested state transition. They are: "IN PROGRESS", "COMPLETED", or "CANCELLED".
    private static readonly IReadOnlySet<ProcedureStepState> AllowedTargetStatesForWorkitemChangeState =
        new HashSet<ProcedureStepState> { ProcedureStepState.InProgress, ProcedureStepState.Canceled, ProcedureStepState.Completed };

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
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.WorkitemInstanceNotFound, workitemInstanceUid),
                    dataset);

                return _responseBuilder.BuildChangeWorkitemStateResponse();
            }

            var originalBlobDicomDataset = await _workitemOrchestrator
                .GetWorkitemBlobAsync(workitemMetadata, cancellationToken)
                .ConfigureAwait(false);

            ValidateChangeWorkitemStateRequest(dataset, workitemMetadata);

            var updateDataset = PrepareChangeWorkitemStateDicomDataset(originalBlobDicomDataset, dataset);

            await _workitemOrchestrator.UpdateWorkitemStateAsync(
                    updateDataset,
                    workitemMetadata,
                    dataset.GetProcedureStepState(),
                    cancellationToken)
                .ConfigureAwait(false);

            _responseBuilder.AddSuccess(string.Format(
                CultureInfo.InvariantCulture,
                DicomCoreResource.WorkitemChangeStateRequestSuccess,
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepStateStringValue));
        }
        catch (Exception ex)
        {
            ushort? failureCode = FailureReasonCodes.ProcessingFailure;
            switch (ex)
            {
                case BadRequestException:
                case DicomValidationException:
                case DatasetValidationException:
                case ValidationException:
                    failureCode = FailureReasonCodes.ValidationFailure;
                    break;

                case WorkitemUpdateNotAllowedException:
                    failureCode = FailureReasonCodes.UpsInstanceUpdateNotAllowed;
                    break;

                case ItemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;
            }

            _logger.LogInformation(ex,
                "Change workitem state failed for the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

            _responseBuilder.AddFailure(failureCode, ex.Message);
        }

        return _responseBuilder.BuildChangeWorkitemStateResponse();
    }

    private static DicomDataset PrepareChangeWorkitemStateDicomDataset(DicomDataset dataset, DicomDataset originalBlobDicomDataset)
    {
        var resultDataset = originalBlobDicomDataset
            .AddOrUpdate(DicomTag.TransactionUID, dataset.GetString(DicomTag.TransactionUID))
            .AddOrUpdate(DicomTag.ProcedureStepState, dataset.GetString(DicomTag.ProcedureStepState));

        return resultDataset;
    }

    private static void ValidateChangeWorkitemStateRequest(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata)
    {
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        // Check for missing Transaction UID
        if (!dataset.TryGetString(DicomTag.TransactionUID, out var transactionUid)
            || string.IsNullOrWhiteSpace(transactionUid))
        {
            throw new BadRequestException(
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.MissingRequiredTag, DicomTag.TransactionUID));
        }

        // Check for missing Procedure Step State
        if (!dataset.TryGetString(DicomTag.ProcedureStepState, out var targetProcedureStepStateStringValue)
            || string.IsNullOrWhiteSpace(targetProcedureStepStateStringValue))
        {
            throw new BadRequestException(
                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.MissingRequiredTag, DicomTag.ProcedureStepState));
        }

        // Check for allowed procedure step state value
        var targetProcedureStepState = ProcedureStepStateExtensions.GetProcedureStepState(targetProcedureStepStateStringValue);
        if (!AllowedTargetStatesForWorkitemChangeState.Contains(targetProcedureStepState))
        {
            throw new BadRequestException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.UnexpectedValue,
                    targetProcedureStepStateStringValue,
                    string.Join(@",", AllowedTargetStatesForWorkitemChangeState)));
        }

        // Check for the workitem final state
        if (workitemMetadata.ProcedureStepState == ProcedureStepState.Canceled ||
            workitemMetadata.ProcedureStepState == ProcedureStepState.Completed)
        {
            throw new WorkitemUpdateNotAllowedException(
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepStateStringValue);
        }

        // Check for the transition state rule validity
        var actionEvent = (targetProcedureStepState == ProcedureStepState.InProgress)
            ? WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID
            : (targetProcedureStepState == ProcedureStepState.Canceled)
                ? WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID
                : WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID;

        var calculatedTransitionState = ProcedureStepStateExtensions.GetTransitionState(workitemMetadata.ProcedureStepState, actionEvent);
        if (calculatedTransitionState.IsError)
        {
            throw new BadRequestException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.WorkitemChangeStateRequestRejected,
                    workitemMetadata.WorkitemUid,
                    targetProcedureStepStateStringValue,
                    calculatedTransitionState.Code));
        }
    }
}
