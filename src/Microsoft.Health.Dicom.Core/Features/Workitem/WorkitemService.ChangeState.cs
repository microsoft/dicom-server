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
    public async Task<ChangeWorkitemStateResponse> ProcessChangeStateAsync(
        DicomDataset dataset,
        string workitemInstanceUid,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemInstanceUid, nameof(workitemInstanceUid));

        try
        {
            GetValidator<ChangeWorkitemStateDatasetValidator>().Validate(dataset);

            var workitemMetadata = await _workitemOrchestrator
                .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
                .ConfigureAwait(false);
            if (workitemMetadata == null)
            {
                _responseBuilder.AddFailure(
                    FailureReasonCodes.UpsInstanceNotFound,
                    DicomCoreResource.WorkitemInstanceNotFound,
                    dataset);

                return _responseBuilder.BuildChangeWorkitemStateResponse();
            }

            var transitionStateResult = ValidateChangeWorkitemStateRequest(dataset, workitemMetadata);
            var originalBlobDicomDataset = await _workitemOrchestrator
                .GetWorkitemBlobAsync(workitemMetadata, cancellationToken)
                .ConfigureAwait(false);

            var updateDataset = GetPreparedChangeWorkitemStateDicomDataset(dataset, originalBlobDicomDataset);
            var targetProcedureStepState = updateDataset.GetProcedureStepState();

            updateDataset.ValidateFinalStateRequirement();

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
                await _workitemOrchestrator.UpdateWorkitemStateAsync(
                        updateDataset,
                        workitemMetadata,
                        dataset.GetProcedureStepState(),
                        cancellationToken)
                    .ConfigureAwait(false);

                _responseBuilder.AddSuccess(string.Empty);
            }
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

    private static DicomDataset GetPreparedChangeWorkitemStateDicomDataset(DicomDataset dataset, DicomDataset originalBlobDicomDataset)
    {
        var resultDataset = originalBlobDicomDataset
            .AddOrUpdate(DicomTag.TransactionUID, dataset.GetString(DicomTag.TransactionUID))
            .AddOrUpdate(DicomTag.ProcedureStepState, dataset.GetString(DicomTag.ProcedureStepState));

        return resultDataset;
    }

    private static WorkitemStateTransitionResult ValidateChangeWorkitemStateRequest(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata)
    {
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        return ChangeWorkitemStateDatasetValidator.ValidateWorkitemState(dataset, workitemMetadata);
    }
}
