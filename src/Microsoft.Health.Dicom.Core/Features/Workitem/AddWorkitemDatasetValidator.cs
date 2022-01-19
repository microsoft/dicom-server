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
            EnsureRequiredTagIsPresent(DicomTag.ScheduledProcedureStepPriority);
            EnsureRequiredTagIsPresent(DicomTag.ProcedureStepLabel);
            EnsureRequiredTagIsPresent(DicomTag.WorklistLabel);
            EnsureRequiredTagIsPresent(DicomTag.ScheduledProcedureStepStartDateTime);
            EnsureRequiredTagIsPresent(DicomTag.ExpectedCompletionDateTime);
            EnsureRequiredTagIsPresent(DicomTag.InputReadinessState);
            EnsureRequiredTagIsPresent(DicomTag.PatientName);
            EnsureRequiredTagIsPresent(DicomTag.PatientID);
            EnsureRequiredTagIsPresent(DicomTag.PatientBirthDate);
            EnsureRequiredTagIsPresent(DicomTag.PatientSex);
            EnsureRequiredTagIsPresent(DicomTag.AdmissionID);
            EnsureRequiredTagIsPresent(DicomTag.AccessionNumber);
            EnsureRequiredTagIsPresent(DicomTag.RequestedProcedureID);
            EnsureRequiredTagIsPresent(DicomTag.RequestingService);
            EnsureRequiredTagIsPresent(DicomTag.ProcedureStepState);
            EnsureRequiredSequenceTagIsPresent(DicomTag.IssuerOfAdmissionIDSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ReferencedRequestSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.IssuerOfAccessionNumberSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ScheduledWorkitemCodeSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ScheduledStationNameCodeSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ScheduledStationClassCodeSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ScheduledStationGeographicLocationCodeSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ScheduledHumanPerformersSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.HumanPerformerCodeSequence);
            EnsureRequiredSequenceTagIsPresent(DicomTag.ReplacedProcedureStepSequence);

            // The format of the identifiers will be validated by fo-dicom.
            string workitemUid = EnsureRequiredTagIsPresent(DicomTag.AffectedSOPInstanceUID);

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

            string EnsureRequiredTagIsPresent(DicomTag dicomTag)
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

            DicomSequence EnsureRequiredSequenceTagIsPresent(DicomTag dicomTag)
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
}
