// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
    /// </summary>
    public class AddWorkitemDatasetValidator : WorkitemDatasetValidator
    {
        protected override void OnValidate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            ValidateAffectedSOPInstanceUID(dicomDataset, workitemInstanceUid);

            ValidateProcedureStepState(dicomDataset, workitemInstanceUid, ProcedureStepState.Scheduled);

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
