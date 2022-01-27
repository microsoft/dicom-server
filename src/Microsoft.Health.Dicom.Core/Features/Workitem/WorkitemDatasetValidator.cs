// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public abstract class WorkitemDatasetValidator : IWorkitemDatasetValidator
    {
        public string Name => GetType().Name;

        public void Validate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            OnValidate(dicomDataset, workitemInstanceUid);
        }

        protected abstract void OnValidate(DicomDataset dicomDataset, string workitemInstanceUid);

        protected static void ValidateAffectedSOPInstanceUID(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            // The format of the identifiers will be validated by fo-dicom.
            string workitemUid = EnsureRequiredTagIsPresent(dicomDataset, DicomTag.AffectedSOPInstanceUID);

            // If the workitemInstanceUid is specified, then the workitemUid must match.
            if (!string.IsNullOrWhiteSpace(workitemInstanceUid) &&
                !workitemUid.Equals(workitemInstanceUid, StringComparison.OrdinalIgnoreCase))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.MismatchWorkitemInstanceUid,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchWorkitemInstanceUid,
                        workitemUid,
                        workitemInstanceUid));
            }
        }

        protected static void ValidateProcedureStepState(DicomDataset dicomDataset, string workitemInstanceUid, string futureState)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            if (dicomDataset.TryGetString(DicomTag.ProcedureStepState, out var currentState) &&
                !ProcedureStepState.CanTransition(currentState, futureState))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.InvalidProcedureStepState,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidProcedureStepState,
                        currentState,
                        workitemInstanceUid));
            }
        }

        protected static string EnsureRequiredTagIsPresent(DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));

            if (dicomDataset.TryGetSingleValue(dicomTag, out string value))
            {
                return value;
            }

            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.MissingRequiredTag,
                    dicomTag.ToString()));
        }

        protected static DicomSequence EnsureRequiredSequenceTagIsPresent(DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            if (dicomTag.GetDefaultVR().Code == DicomVRCode.SQ && dicomDataset.TryGetSequence(dicomTag, out var sequence))
            {
                return sequence;
            }

            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.MissingRequiredTag,
                    dicomTag.ToString()));
        }

    }
}
