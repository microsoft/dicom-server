// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
    /// </summary>
    public class AddWorkitemDatasetValidator : IAddWorkitemDatasetValidator
    {
        public void Validate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            ValidateAffectedSOPInstanceUID(dicomDataset, workitemInstanceUid);

            ValidateProcedureStepState(dicomDataset, workitemInstanceUid);

            ValidateRequiredTags(dicomDataset);
        }

        private static void ValidateRequiredTags(DicomDataset dicomDataset)
        {
            // Ensure required tags are present.
            foreach (DicomTag tag in GetWorkitemRequiredTags())
            {
                EnsureRequiredTagIsPresent(dicomDataset, tag);
            }

            // Ensure required sequence tags are present
            foreach (DicomTag tag in GetWorkitemRequiredSequenceTags())
            {
                EnsureRequiredSequenceTagIsPresent(dicomDataset, tag);
            }
        }

        private static void ValidateProcedureStepState(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            if (dicomDataset.TryGetString(DicomTag.ProcedureStepState, out var currentState) &&
                !ProcedureStepState.CanTransition(currentState, ProcedureStepState.Scheduled))
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

        private static void ValidateAffectedSOPInstanceUID(DicomDataset dicomDataset, string workitemInstanceUid)
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

        private static string EnsureRequiredTagIsPresent(DicomDataset dicomDataset, DicomTag dicomTag)
        {
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

        private static DicomSequence EnsureRequiredSequenceTagIsPresent(DicomDataset dicomDataset, DicomTag dicomTag)
        {
            if (dicomTag.GetDefaultVR().Code == DicomVRCode.SQ)
            {
                dicomDataset.TryGetSequence(dicomTag, out var sequence);
                return sequence;
            }

            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.MissingRequiredTag,
                    dicomTag.ToString()));
        }

        internal static IEnumerable<DicomTag> GetWorkitemRequiredTags()
        {
            yield return DicomTag.ScheduledProcedureStepPriority;
            yield return DicomTag.ProcedureStepLabel;
            yield return DicomTag.WorklistLabel;
            yield return DicomTag.ScheduledProcedureStepStartDateTime;
            yield return DicomTag.ExpectedCompletionDateTime;
            yield return DicomTag.InputReadinessState;
            yield return DicomTag.PatientName;
            yield return DicomTag.PatientID;
            yield return DicomTag.PatientBirthDate;
            yield return DicomTag.PatientSex;
            yield return DicomTag.AdmissionID;
            yield return DicomTag.AccessionNumber;
            yield return DicomTag.RequestedProcedureID;
            yield return DicomTag.RequestingService;
            yield return DicomTag.ProcedureStepState;
        }

        internal static IEnumerable<DicomTag> GetWorkitemRequiredSequenceTags()
        {
            yield return DicomTag.IssuerOfAdmissionIDSequence;
            yield return DicomTag.ReferencedRequestSequence;
            yield return DicomTag.IssuerOfAccessionNumberSequence;
            yield return DicomTag.ScheduledWorkitemCodeSequence;
            yield return DicomTag.ScheduledStationNameCodeSequence;
            yield return DicomTag.ScheduledStationClassCodeSequence;
            yield return DicomTag.ScheduledStationGeographicLocationCodeSequence;
            yield return DicomTag.ScheduledHumanPerformersSequence;
            yield return DicomTag.HumanPerformerCodeSequence;
            yield return DicomTag.ReplacedProcedureStepSequence;
        }
    }
}
