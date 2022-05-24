// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

public sealed class ChangeWorkitemStateDatasetValidator : WorkitemDatasetValidator
{
    // The legal values correspond to the requested state transition. They are: "IN PROGRESS", "COMPLETED", or "CANCELLED".
    private static readonly IReadOnlySet<ProcedureStepState> AllowedTargetStatesForWorkitemChangeState =
        new HashSet<ProcedureStepState> { ProcedureStepState.InProgress, ProcedureStepState.Canceled, ProcedureStepState.Completed };

    protected override void OnValidate(DicomDataset dataset)
    {
        // Check for missing Transaction UID
        dataset.ValidateRequirement(DicomTag.TransactionUID, RequirementCode.OneOne);

        // Check for missing Procedure Step State
        dataset.ValidateRequirement(DicomTag.ProcedureStepState, RequirementCode.OneOne);

        // Check for allowed procedure step state value
        var targetProcedureStepStateStringValue = dataset.GetString(DicomTag.ProcedureStepState);
        var targetProcedureStepState = ProcedureStepStateExtensions.GetProcedureStepState(targetProcedureStepStateStringValue);
        if (!AllowedTargetStatesForWorkitemChangeState.Contains(targetProcedureStepState))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.UnexpectedValue,
                    DicomTag.ProcedureStepState,
                    string.Join(@",", AllowedTargetStatesForWorkitemChangeState)));
        }
    }

    /// <summary>
    /// Validates Workitem state in the store and procedure step state transition validity.
    ///
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata status is not read-write.
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata transition state has error.
    /// 
    /// </summary>
    /// <param name="dataset">The Change Workitem State request DICOM dataset</param>
    /// <param name="workitemMetadata">The Workitem Metadata</param>
    internal static void ValidateWorkitemState(DicomDataset dataset, WorkitemMetadataStoreEntry workitemMetadata)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        // the request Transaction UID must match the current Transaction UID.
        var transactionUid = dataset.GetString(DicomTag.TransactionUID);
        if (!string.IsNullOrWhiteSpace(workitemMetadata.TransactionUid) &&
            !string.Equals(workitemMetadata.TransactionUid, transactionUid, System.StringComparison.Ordinal))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                DicomCoreResource.InvalidTransactionUID);
        }

        // Check for the transition state rule validity
        var targetProcedureStepStateStringValue = dataset.GetString(DicomTag.ProcedureStepState);
        var targetProcedureStepState = ProcedureStepStateExtensions.GetProcedureStepState(targetProcedureStepStateStringValue);

        var actionEvent = (targetProcedureStepState == ProcedureStepState.InProgress)
            ? WorkitemStateEvents.NActionToInProgressWithCorrectTransactionUID
            : (targetProcedureStepState == ProcedureStepState.Canceled)
                ? WorkitemStateEvents.NActionToCanceledWithCorrectTransactionUID
                : WorkitemStateEvents.NActionToCompletedWithCorrectTransactionUID;

        // Check the state transition validity
        var calculatedTransitionState = ProcedureStepStateExtensions.GetTransitionState(workitemMetadata.ProcedureStepState, actionEvent);
        if (calculatedTransitionState.IsError)
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.InvalidProcedureStepStateTransition,
                    targetProcedureStepStateStringValue,
                    calculatedTransitionState.Code));
        }

        // Check if the workitem is locked for read-write
        if (workitemMetadata.Status != WorkitemStoreStatus.ReadWrite)
        {
            throw new DatasetValidationException(
                FailureReasonCodes.UpsInstanceUpdateNotAllowed,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.WorkitemUpdateIsNotAllowed,
                    workitemMetadata.ProcedureStepStateStringValue));
        }
    }
}
