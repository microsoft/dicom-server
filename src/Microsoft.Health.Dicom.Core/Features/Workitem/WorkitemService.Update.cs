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
    public async Task<UpdateWorkitemResponse> ProcessUpdateAsync(DicomDataset dataset, string workitemInstanceUid, string transactionUid, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotEmptyOrWhiteSpace(workitemInstanceUid, nameof(workitemInstanceUid));

        var workitemMetadata = await _workitemOrchestrator
            .GetWorkitemMetadataAsync(workitemInstanceUid, cancellationToken)
            .ConfigureAwait(false);

        // If workitem metadata is not found in SQL DB, return UpsInstanceNotFound failure.
        if (workitemMetadata == null)
        {
            _responseBuilder.AddFailure(
                FailureReasonCodes.UpsInstanceNotFound,
                string.Format(DicomCoreResource.WorkitemInstanceNotFound, workitemInstanceUid),
                dataset);
            return _responseBuilder.BuildUpdateWorkitemResponse();
        }

        // Validate the following:
        //  1. If state is SCHEDULED, transaction UID is not provided.
        //  2. If state is IN PROGRESS, provided transaction UID matches the existing transaction UID.
        //  3. State is not COMPLETED or CANCELED.
        //  4. Dataset is valid.
        if (ValidateUpdateRequest(dataset, workitemMetadata, transactionUid))
        {
            await UpdateWorkitemAsync(dataset, workitemMetadata, cancellationToken);
        }

        return _responseBuilder.BuildUpdateWorkitemResponse();
    }

    /// <summary>
    /// Validate the following:
    ///  1. If state is SCHEDULED, transaction UID is not provided.
    ///  2. If state is IN PROGRESS, provided transaction UID matches the existing transaction UID.
    ///  3. State is not COMPLETED or CANCELED.
    ///  4. Dataset is valid.
    /// </summary>
    /// <param name="dataset">Incoming dataset to be validated.</param>
    /// <param name="workitemMetadata">Workitem metadata.</param>
    /// <param name="transactionUid">Transaction UID.</param>
    /// <returns>True if validated, else return false.</returns>
    private bool ValidateUpdateRequest(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata, string transactionUid)
    {
        try
        {
            UpdateWorkitemDatasetValidator.ValidateWorkitemStateAndTransactionUid(transactionUid, workitemMetadata);

            GetValidator<UpdateWorkitemDatasetValidator>().Validate(dataset);

            return true;
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            switch (ex)
            {
                case DatasetValidationException dicomDatasetValidationException:
                    failureCode = dicomDatasetValidationException.FailureCode;
                    break;

                case DicomValidationException:
                case ValidationException:
                    failureCode = FailureReasonCodes.ValidationFailure;
                    break;

                case WorkitemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;
            }

            _logger.LogInformation(ex, "Validation failed for the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

            _responseBuilder.AddFailure(failureCode, ex.Message, dataset);

            return false;
        }
    }

    private async Task UpdateWorkitemAsync(
        DicomDataset dataset,
        WorkitemMetadataStoreEntry workitemMetadata,
        CancellationToken cancellationToken)
    {
        try
        {
            await _workitemOrchestrator
                .UpdateWorkitemAsync(dataset, workitemMetadata, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Successfully updated the DICOM instance work-item entry.");

            _responseBuilder.AddSuccess(DicomCoreResource.WorkitemUpdateRequestSuccess);
        }
        catch (Exception ex)
        {
            ushort failureCode = FailureReasonCodes.ProcessingFailure;

            // TODO Ali: See if other failure codes need to be added here.
            switch (ex)
            {
                case DatasetValidationException dvEx:
                    failureCode = dvEx.FailureCode;
                    break;

                case DicomValidationException:
                case ValidationException:
                case WorkitemUpdateNotAllowedException:
                    failureCode = FailureReasonCodes.UpsInstanceUpdateNotAllowed;
                    break;

                case WorkitemNotFoundException:
                    failureCode = FailureReasonCodes.UpsInstanceNotFound;
                    break;
            }

            _logger.LogWarning(ex, "Failed to update the DICOM instance work-item entry. Failure code: {FailureCode}.", failureCode);

            _responseBuilder.AddFailure(failureCode, ex.Message, dataset);
        }
    }
}
