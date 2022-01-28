// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;

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

            ValidateRequiredTags(dicomDataset);

            ValidateAffectedSOPInstanceUID(dicomDataset, workitemInstanceUid);

            ValidateProcedureStepState(dicomDataset, workitemInstanceUid);

            ValidateTransactionUID(dicomDataset, workitemInstanceUid);

            ValidateForDuplicateTagValuesInSequence(dicomDataset);
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

        private static void ValidateForDuplicateTagValuesInSequence(DicomDataset dicomDataset)
        {
            var sequenceList = dicomDataset.Where(t => t.ValueRepresentation == DicomVR.SQ).Cast<DicomSequence>();

            var tagValueMap = new Dictionary<string, string>();
            foreach (var sequence in sequenceList)
            {
                tagValueMap.Clear();

                foreach (var item in sequence.Items.SelectMany(ds => ds))
                {
                    var tagPath = item.Tag.GetPath();

                    if (dicomDataset.TryGetString(item.Tag, out var tagValue) &&
                        tagValueMap.ContainsKey(tagPath) &&
                        string.Equals(tagValue, tagValueMap[tagPath], StringComparison.Ordinal))
                    {
                        throw new DatasetValidationException(
                            FailureReasonCodes.ValidationFailure,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                DicomCoreResource.DuplicateTagValueNotSupported,
                                tagValue,
                                tagPath));
                    }

                    tagValueMap[tagPath] = tagValue;
                }
            }
        }

        private static void ValidateTransactionUID(DicomDataset dicomDataset, string workitemInstanceUid)
        {
            // ProcedureStepState should be empty for create
            if (dicomDataset.TryGetString(DicomTag.TransactionUID, out var transactionUID) && !string.IsNullOrEmpty(transactionUID))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.InvalidTransactionUID,
                        transactionUID,
                        workitemInstanceUid));
            }
        }

        internal static IEnumerable<DicomTag> GetWorkitemRequiredTags()
        {
            yield return DicomTag.WorklistLabel;
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
            yield return DicomTag.ProcedureStepLabel;
            yield return DicomTag.ScheduledProcedureStepPriority;
            yield return DicomTag.ScheduledProcedureStepStartDateTime;
            // yield return DicomTag.ProcedureStepState;
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
