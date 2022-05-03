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
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to process the search request based on the Workitem Instance UID.
/// </summary>
public partial class WorkitemService
{
    /// <inheritdoc />
    public async Task<RetrieveWorkitemResponse> ProcessRetrieveAsync(string workitemInstanceUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemInstanceUid, nameof(workitemInstanceUid));

        try
        {
            var workitemMetadata = await _workitemOrchestrator
                .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
                .ConfigureAwait(false);

            ValidateRetrieveRequest(workitemMetadata);

            var dicomDataset = await _workitemOrchestrator
                .RetrieveWorkitemAsync(workitemMetadata, cancellationToken)
                .ConfigureAwait(false);

            PrepareRetrieveResponseDicomDataset(dicomDataset);

            _responseBuilder.AddSuccess(dicomDataset);

            _logger.LogInformation("Successfully retrieved the DICOM instance work-item entry.");
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            _logger.LogWarning(ex, "Failed to retrieve the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);
            switch (ex)
            {
                case DataStoreException:
                case ItemNotFoundException:
                case WorkitemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;
                case WorkitemIsInFinalStateException:
                    failureCode = (ex as WorkitemIsInFinalStateException).ErrorOrWarningCode;
                    break;
            }

            _responseBuilder.AddFailure(failureCode, ex.Message);
        }

        return _responseBuilder.BuildRetrieveWorkitemResponse();
    }

    private static void ValidateRetrieveRequest(WorkitemMetadataStoreEntry workitemMetadata)
    {
        if (workitemMetadata == null)
        {
            throw new WorkitemNotFoundException(workitemMetadata.WorkitemUid);
        }

        var procedureStepState = workitemMetadata.ProcedureStepState;
        if (procedureStepState == ProcedureStepState.Completed)
        {
            throw new WorkitemIsInFinalStateException(
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepStateStringValue,
                FailureReasonCodes.UpsIsAlreadyCompleted);
        }
        if (procedureStepState == ProcedureStepState.Canceled)
        {
            throw new WorkitemIsInFinalStateException(
                workitemMetadata.WorkitemUid,
                workitemMetadata.ProcedureStepStateStringValue,
                FailureReasonCodes.UpsIsAlreadyCanceled);
        }
    }

    private static void PrepareRetrieveResponseDicomDataset(DicomDataset dataset)
    {
        // alaways remove Transaction UID from the result dicomDataset.
        dataset.Remove(DicomTag.TransactionUID);
    }
}
