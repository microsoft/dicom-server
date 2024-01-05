// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
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
    // The legal values correspond to the requested state transition. They are: "IN PROGRESS", "COMPLETED", or "CANCELED".
    private static readonly ImmutableHashSet<string> AllowedTargetStatesForWorkitemChangeState = ImmutableHashSet.Create(
        ProcedureStepStateConstants.InProgress,
        ProcedureStepStateConstants.Canceled,
        ProcedureStepStateConstants.Completed);

    protected override void OnValidate(DicomDataset dataset)
    {
        // Check for missing Transaction UID
        dataset.ValidateRequirement(DicomTag.TransactionUID, RequirementCode.OneOne);

        // Check for missing Procedure Step State
        dataset.ValidateRequirement(DicomTag.ProcedureStepState, RequirementCode.OneOne);

        // Check for allowed procedure step state value
        var targetProcedureStepStateStringValue = dataset.GetString(DicomTag.ProcedureStepState);
        if (!AllowedTargetStatesForWorkitemChangeState.Contains(targetProcedureStepStateStringValue))
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
    /// Validates that the passed Transaction UID matches against the workitem if it already has a Transaction UID
    /// Otherwise, treats it as a new Transaction UID.
    ///
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata status is not read-write.
    /// Throws <see cref="DatasetValidationException"/> when the workitem-metadata transition state has error.
    /// 
    /// </summary>
    /// <param name="requestDataset">The Change Workitem State request DICOM dataset</param>
    /// <param name="workitemMetadata">The Workitem Metadata</param>
    internal static WorkitemStateTransitionResult ValidateWorkitemState(DicomDataset requestDataset, WorkitemMetadataStoreEntry workitemMetadata)
    {
        EnsureArg.IsNotNull(requestDataset, nameof(requestDataset));
        EnsureArg.IsNotNull(workitemMetadata, nameof(workitemMetadata));

        // Check for the transition state rule validity
        var targetProcedureStepStateStringValue = requestDataset.GetString(DicomTag.ProcedureStepState);
        var targetProcedureStepState = ProcedureStepStateExtensions.GetProcedureStepState(targetProcedureStepStateStringValue);

        // the request Transaction UID must match the current Transaction UID.
        var transactionUid = requestDataset.GetString(DicomTag.TransactionUID);
        var hasMatchingTransactionUid = string.Equals(workitemMetadata.TransactionUid, transactionUid, System.StringComparison.Ordinal);
        var hasNewTransactionUid =
            string.IsNullOrWhiteSpace(workitemMetadata.TransactionUid) &&
            targetProcedureStepState == ProcedureStepState.InProgress;

        WorkitemActionEvent actionEvent = WorkitemActionEvent.NActionToInProgress;
        switch (targetProcedureStepState)
        {
            case ProcedureStepState.Canceled:
                actionEvent = WorkitemActionEvent.NActionToCanceled;
                break;
            case ProcedureStepState.Completed:
                actionEvent = WorkitemActionEvent.NActionToCompleted;
                break;
        }

        // Check the state transition validity
        var calculatedTransitionState = ProcedureStepStateExtensions.GetTransitionState(
            workitemMetadata.ProcedureStepState,
            actionEvent,
            hasNewTransactionUid || hasMatchingTransactionUid);

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

        return calculatedTransitionState;
    }
}
