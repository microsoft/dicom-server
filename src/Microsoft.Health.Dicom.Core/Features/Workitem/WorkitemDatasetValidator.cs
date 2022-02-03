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

        protected static void ValidateWorkitemInstanceUid(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // The format of the identifiers will be validated by fo-dicom.
            var hasWorkitemInstanceUid = !string.IsNullOrEmpty(workitemInstanceUid);
            var hasSopInstanceUid = dicomDataset.TryGetString(DicomTag.SOPInstanceUID, out var sopInstanceUid);
            var hasAffectedSOPInstanceUID = dicomDataset.TryGetString(DicomTag.AffectedSOPInstanceUID, out var affectedSopInstanceUid);

            if (string.IsNullOrWhiteSpace(workitemInstanceUid))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MissingWorkitemInstanceUid));
            }

            // if the workitemInstanceUid is available in SOPInstanceUid, check against the WorkitemInstanceUid that came in the Url
            if (hasSopInstanceUid && hasWorkitemInstanceUid && !AreSame(workitemInstanceUid, sopInstanceUid))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchSopInstanceWorkitemInstanceUid,
                        sopInstanceUid,
                        workitemInstanceUid));
            }

            // if the workitemInstanceUid is available in AffectedSOPInstanceUid, check against the WorkitemInstanceUid that came in the Url
            if (hasAffectedSOPInstanceUID && hasWorkitemInstanceUid && !AreSame(workitemInstanceUid, affectedSopInstanceUid))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MismatchAffectedSopInstanceWorkitemInstanceUid,
                        affectedSopInstanceUid,
                        workitemInstanceUid));
            }
        }

        private static bool AreSame(string valueX, string valueY)
        {
            if (string.IsNullOrWhiteSpace(valueX) && string.IsNullOrWhiteSpace(valueY))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(valueX) && !string.IsNullOrWhiteSpace(valueY))
            {
                return false;
            }

            return string.Equals(valueX, valueY, StringComparison.Ordinal);
        }

        protected static void ValidateProcedureStepState(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            if (dicomDataset.TryGetString(DicomTag.ProcedureStepState, out var currentState))
            {
                var result = ProcedureStepState.GetTransitionState(WorkitemStateEvents.NCreate, currentState);
                if (result.IsError)
                {
                    throw new DatasetValidationException(
                        FailureReasonCodes.ValidationFailure,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DicomCoreResource.InvalidProcedureStepState,
                            currentState,
                            workitemInstanceUid,
                            result.Code));
                }
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
