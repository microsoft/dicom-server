// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Updating.
/// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 5.4.2.1</see>
/// </summary>
public class UpdateWorkitemDatasetValidator : WorkitemDatasetValidator
{
    /// <summary>
    /// Validate requirement codes for dicom tags based on the spec.
    /// Reference: <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3"/>
    /// </summary>
    /// <param name="dataset">Dataset to be validated.</param>
    protected override void OnValidate(DicomDataset dataset)
    {
        dataset.ValidateAllRequirements(WorkitemRequestType.Update);
    }

    /// <summary>
    /// Validates Workitem state in the store and procedure step state transition validity.
    /// Also validate that the passed Transaction Uid matches the existing transaction Uid.
    /// 
    /// Throws <see cref="WorkitemNotFoundException"/> when workitem-metadata is null.
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata status is not read-write.
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata procedure step state is not In Progress.
    /// Throws <see cref="DatasetValidationException"/> when the transaction uid does not match the existing transaction uid.
    /// 
    /// </summary>
    /// <param name="transactionUid">The Transaction Uid.</param>
    /// <param name="workitemMetadata">The Workitem Metadata.</param>
    public static void ValidateWorkitemStateAndTransactionUid(string transactionUid, WorkitemMetadataStoreEntry workitemMetadata)
    {
        if (workitemMetadata == null)
        {
            throw new WorkitemNotFoundException();
        }

        switch (workitemMetadata.ProcedureStepState)
        {
            case ProcedureStepState.Scheduled:
                //  Update can be made when in Scheduled state. Transaction UID cannot be present though.
                if (!string.IsNullOrWhiteSpace(transactionUid))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidIncorrect,
                        DicomCoreResource.InvalidTransactionUID);
                }
                break;
            case ProcedureStepState.InProgress:
                // Transaction UID must be provided
                if (string.IsNullOrWhiteSpace(transactionUid))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidAbsent,
                        DicomCoreResource.InvalidWorkitemInstanceTargetUri);
                }

                // Provided Transaction UID has to be equal to the existing Transaction UID.
                if (!string.Equals(workitemMetadata.TransactionUid, transactionUid, System.StringComparison.Ordinal))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidIncorrect,
                        DicomCoreResource.InvalidWorkitemInstanceTargetUri);
                }

                break;
            case ProcedureStepState.Completed:
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCompleted,
                    string.Concat(DicomCoreResource.UpdateWorkitemInstanceConflictFailure, " ", DicomCoreResource.WorkitemIsAlreadyCompleted));

            case ProcedureStepState.Canceled:
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCanceled,
                    string.Concat(DicomCoreResource.UpdateWorkitemInstanceConflictFailure, " ", DicomCoreResource.WorkitemIsAlreadyCanceled));
        }
    }
}
