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
/// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
/// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 3.4.5.4.2.1</see>
/// </summary>
public class UpdateWorkitemDatasetValidator : WorkitemDatasetValidator
{
    /// <summary>
    /// Validate requirements for update-workitem.
    /// Some values are not allowed which are checked explicitly.
    /// All other values, if present, must not be empty.
    /// Reference: https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3
    /// </summary>
    /// <param name="dataset">Dataset to be validated.</param>
    protected override void OnValidate(DicomDataset dataset)
    {
        // if transaction UID is present, make sure it is not empty.
        ValidateNotEmptyValue(dataset, DicomTag.TransactionUID);

        // SOP Common Module
        // TODO: validate character set
        ValidateNotPresent(dataset, DicomTag.SOPClassUID);
        ValidateNotPresent(dataset, DicomTag.SOPInstanceUID);

        // Unified Procedure Step Scheduled Procedure Information Module
        // If either of these values are present, make sure they are not empty.
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledProcedureStepPriority);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledProcedureStepModificationDateTime);
        ValidateNotEmptyValue(dataset, DicomTag.ProcedureStepLabel);
        ValidateNotEmptyValue(dataset, DicomTag.WorklistLabel);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledProcessingParametersSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledStationNameCodeSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledStationNameCodeSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledStationClassCodeSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledStationGeographicLocationCodeSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledStationNameCodeSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledProcedureStepStartDateTime);
        ValidateNotEmptyValue(dataset, DicomTag.ScheduledWorkitemCodeSequence);
        ValidateNotEmptyValue(dataset, DicomTag.CommentsOnTheScheduledProcedureStep);
        ValidateNotEmptyValue(dataset, DicomTag.InputReadinessState);
        ValidateNotEmptyValue(dataset, DicomTag.InputInformationSequence);

        // Unified Procedure Step Relationship Module
        ValidateNotPresent(dataset, DicomTag.PatientName);

        // Issuer of Patient ID Macro
        ValidateNotPresent(dataset, DicomTag.IssuerOfPatientID);
        ValidateNotPresent(dataset, DicomTag.IssuerOfPatientIDQualifiersSequence);

        ValidateNotPresent(dataset, DicomTag.OtherPatientIDsSequence);
        ValidateNotPresent(dataset, DicomTag.PatientBirthDate);
        ValidateNotPresent(dataset, DicomTag.PatientSex);
        ValidateNotPresent(dataset, DicomTag.AdmissionID);
        ValidateNotPresent(dataset, DicomTag.IssuerOfAdmissionIDSequence);
        ValidateNotPresent(dataset, DicomTag.AdmittingDiagnosesDescription);
        ValidateNotPresent(dataset, DicomTag.AdmittingDiagnosesCodeSequence);
        ValidateNotPresent(dataset, DicomTag.ReferencedRequestSequence);

        // Unified Procedure Step Progress Information Module
        ValidateNotPresent(dataset, DicomTag.ProcedureStepState);
        ValidateNotEmptyValue(dataset, DicomTag.ProcedureStepProgressInformationSequence);
        ValidateNotEmptyValue(dataset, DicomTag.ProcedureStepCancellationDateTime);
        ValidateNotEmptyValue(dataset, DicomTag.UnifiedProcedureStepPerformedProcedureSequence);
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
    public static void ValidateWorkitemStateAndTransactionUid(
        string transactionUid,
        WorkitemMetadataStoreEntry workitemMetadata)
    {
        if (workitemMetadata == null)
        {
            throw new WorkitemNotFoundException();
        }

        if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ProcessingFailure,
                DicomCoreResource.WorkitemCurrentlyBeingUpdated);
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
                        DicomCoreResource.TransactionUIDAbsent);
                }

                // Provided Transaction UID has to be equal to the existing Transaction UID.
                if (!string.Equals(workitemMetadata.TransactionUid, transactionUid, System.StringComparison.Ordinal))
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.UpsTransactionUidIncorrect,
                        DicomCoreResource.InvalidTransactionUID);
                }

                break;
            case ProcedureStepState.Completed:
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCompleted,
                    DicomCoreResource.WorkitemIsAlreadyCompleted);
            case ProcedureStepState.Canceled:
                throw new DatasetValidationException(
                    FailureReasonCodes.UpsIsAlreadyCanceled,
                    DicomCoreResource.WorkitemIsAlreadyCanceled);
        }
    }
}
