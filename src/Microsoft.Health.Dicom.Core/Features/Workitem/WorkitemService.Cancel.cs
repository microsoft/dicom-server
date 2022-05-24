// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    public async Task<CancelWorkitemResponse> ProcessCancelAsync(
        DicomDataset dataset,
        string workitemInstanceUid,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        var workitemMetadata = await _workitemOrchestrator
            .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
            .ConfigureAwait(false);

        if (workitemMetadata == null)
        {
            _responseBuilder.AddFailure(
                FailureReasonCodes.UpsInstanceNotFound,
                DicomCoreResource.WorkitemInstanceNotFound,
                dataset);
            return _responseBuilder.BuildCancelResponse();
        }

        // Get the state transition result
        var transitionStateResult = workitemMetadata
            .ProcedureStepState
            .GetTransitionState(WorkitemStateEvents.NActionToRequestCancel);
        var cancelRequestDataset = await PrepareRequestCancelWorkitemBlobDatasetAsync(
                dataset, workitemMetadata, transitionStateResult.State, cancellationToken)
            .ConfigureAwait(false);

        await ValidateAndCancelWorkitemAsync(
                cancelRequestDataset,
                workitemInstanceUid,
                workitemMetadata,
                transitionStateResult,
                cancellationToken)
            .ConfigureAwait(false);

        return _responseBuilder.BuildCancelResponse();
    }

    private async Task ValidateAndCancelWorkitemAsync(
        DicomDataset cancelRequestDataset,
        string workitemInstanceUid,
        WorkitemMetadataStoreEntry workitemMetadata,
        WorkitemStateTransitionResult transitionStateResult,
        CancellationToken cancellationToken)
    {
        if (ValidateCancelRequest(workitemInstanceUid, workitemMetadata, cancelRequestDataset, transitionStateResult))
        {
            // If there is a warning code, the workitem is already in the canceled state.
            if (transitionStateResult.HasWarningWithCode)
            {
                _responseBuilder.AddSuccess(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.WorkitemIsInFinalState,
                        workitemMetadata.ProcedureStepStateStringValue,
                        transitionStateResult.Code));
            }
            else
            {
                await CancelWorkitemAsync(
                        cancelRequestDataset,
                        workitemMetadata,
                        transitionStateResult.State,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task<DicomDataset> PrepareRequestCancelWorkitemBlobDatasetAsync(
        DicomDataset dataset,
        WorkitemMetadataStoreEntry workitemMetadata,
        ProcedureStepState targetProcedureStepState,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the workitem from blob store
            var workitemDataset = await _workitemOrchestrator
                .GetWorkitemBlobAsync(workitemMetadata, cancellationToken)
                .ConfigureAwait(false);

            PopulateCancelRequestAttributes(workitemDataset, dataset, targetProcedureStepState);

            return workitemDataset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Error while preparing Cancel Request Blob Dataset");

            throw;
        }
    }

    private static DicomDataset PopulateCancelRequestAttributes(
        DicomDataset workitemDataset,
        DicomDataset cancelRequestDataset,
        ProcedureStepState procedureStepState)
    {
        workitemDataset.AddOrUpdate(DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow);
        workitemDataset.AddOrUpdate(DicomTag.ProcedureStepState, procedureStepState.GetStringValue());

        var cancellationReason = cancelRequestDataset.GetSingleValueOrDefault<string>(DicomTag.ReasonForCancellation, string.Empty);
        var discontinuationReasonCodeSequence = new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset
        {
            { DicomTag.ReasonForCancellation, cancellationReason }
        });
        workitemDataset.AddOrUpdate(discontinuationReasonCodeSequence);

        var progressInformationSequence = new DicomSequence(DicomTag.ProcedureStepProgressInformationSequence, new DicomDataset
        {
            { DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow },
            new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset
                {
                    { DicomTag.ReasonForCancellation, cancellationReason }
                }),
            new DicomSequence(DicomTag.ProcedureStepCommunicationsURISequence, new DicomDataset
                {
                    { DicomTag.ContactURI, cancelRequestDataset.GetSingleValueOrDefault<string>(DicomTag.ContactURI, string.Empty) },
                    { DicomTag.ContactDisplayName, cancelRequestDataset.GetSingleValueOrDefault<string>(DicomTag.ContactDisplayName, string.Empty) },
                })
        });
        workitemDataset.AddOrUpdate(progressInformationSequence);

        // TODO: Remove this once Update workitem feature is implemented
        // This is a workaround for Cancel workitem to work without Update workitem
        if (cancelRequestDataset.TryGetSequence(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, out var unifiedProcedureStepPerformedProcedureSequence))
        {
            workitemDataset.AddOrUpdate(unifiedProcedureStepPerformedProcedureSequence);
        }

        return workitemDataset;
    }

    private bool ValidateCancelRequest(
        string workitemInstanceUid,
        WorkitemMetadataStoreEntry workitemMetadata,
        DicomDataset dataset,
        WorkitemStateTransitionResult transitionStateResult)
    {
        try
        {
            CancelWorkitemDatasetValidator.ValidateWorkitemState(
                workitemInstanceUid,
                workitemMetadata,
                transitionStateResult);

            GetValidator<CancelWorkitemDatasetValidator>().Validate(dataset);

            return true;
        }
        catch (Exception ex)
        {
            ushort? failureCode = FailureReasonCodes.ProcessingFailure;

            switch (ex)
            {
                case DatasetValidationException datasetValidationException:
                    failureCode = datasetValidationException.FailureCode;
                    break;

                case DicomValidationException _:
                case ValidationException _:
                    failureCode = FailureReasonCodes.UpsInstanceUpdateNotAllowed;
                    break;

                case WorkitemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;
            }

            _logger.LogInformation(ex,
                "Validation failed for the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

            _responseBuilder.AddFailure(failureCode, ex.Message, dataset);

            return false;
        }
    }

    private async Task CancelWorkitemAsync(
        DicomDataset dataset,
        WorkitemMetadataStoreEntry workitemMetadata,
        ProcedureStepState targetProcedureStepState,
        CancellationToken cancellationToken)
    {
        try
        {
            await _workitemOrchestrator
                .UpdateWorkitemStateAsync(dataset, workitemMetadata, targetProcedureStepState, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Successfully canceled the work-item entry.");

            _responseBuilder.AddSuccess(DicomCoreResource.WorkitemCancelRequestSuccess);
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            switch (ex)
            {
                case WorkitemNotFoundException _:
                    failureCode = FailureReasonCodes.ProcessingFailure;
                    break;
            }

            _logger.LogWarning(ex, "Failed to cancel the work-item entry. Failure code: {FailureCode}.", failureCode);

            _responseBuilder.AddFailure(failureCode, ex.Message, dataset);
        }
    }
}
