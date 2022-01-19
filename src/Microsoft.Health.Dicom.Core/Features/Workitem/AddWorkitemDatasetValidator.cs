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
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement when Adding.
    /// </summary>
    public class AddWorkitemDatasetValidator : IAddWorkitemDatasetValidator
    {
        public void Validate(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            ValidateRequiredTags(dicomDataset, workitemInstanceUid);
        }

        private static void ValidateRequiredTags(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            // Ensure required tags are present.
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.ScheduledProcedureStepPriority);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.ProcedureStepLabel);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.WorklistLabel);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.ScheduledProcedureStepStartDateTime);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.ExpectedCompletionDateTime);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.InputReadinessState);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.PatientName);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.PatientID);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.PatientBirthDate);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.PatientSex);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.AdmissionID);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.AccessionNumber);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.RequestedProcedureID);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.RequestingService);
            EnsureRequiredTagIsPresent(dicomDataset, DicomTag.ProcedureStepState);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.IssuerOfAdmissionIDSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ReferencedRequestSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.IssuerOfAccessionNumberSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ScheduledWorkitemCodeSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ScheduledStationNameCodeSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ScheduledStationClassCodeSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ScheduledStationGeographicLocationCodeSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ScheduledHumanPerformersSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.HumanPerformerCodeSequence);
            EnsureRequiredSequenceTagIsPresent(dicomDataset, DicomTag.ReplacedProcedureStepSequence);

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
    }
}
