// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// 
    /// </summary>
    internal static class WorkitemFinalStateValidatorExtension
    {
        private static readonly HashSet<FinalStateRequirementDetail> Requirements = GetRequirements();

        public static void ValidateFinalStateRequirement(this DicomDataset dataset)
        {
            var procedureStepState = dataset.GetProcedureState();

            foreach (var requirement in Requirements)
            {
                dataset.ValidateRequirement(requirement.DicomTag, procedureStepState, requirement.RequirementCode);

                if (null != requirement.SequenceRequirements)
                {
                    dataset.ValidateSequence(requirement.DicomTag, procedureStepState, requirement.SequenceRequirements);
                }
            }
        }

        private static void ValidateSequence(this DicomDataset dataset, DicomTag sequenceTag, ProcedureStepState procedureStepState, IReadOnlyCollection<FinalStateRequirementDetail> requirements)
        {
            if (requirements.Count == 0)
            {
                return;
            }

            // TODO: Should we consider throwing an exception here?
            if (!dataset.TryGetSequence(sequenceTag, out var sequence))
            {
                return;
            }

            var sequenceDatasets = sequence.Items;

            // TODO: Should we just search for the requirements in the 1st Dataset in the Sequence?
            var sequenceDataset = sequenceDatasets.First();
            foreach (var requirement in requirements)
            {
                sequenceDataset.ValidateRequirement(requirement.DicomTag, procedureStepState, requirement.RequirementCode);

                if (null != requirement.SequenceRequirements)
                {
                    // Recursive call. Should we simplify the levels?
                    sequenceDataset.ValidateSequence(requirement.DicomTag, procedureStepState, requirement.SequenceRequirements);
                }
            }
        }

        /// <summary>
        /// Refer <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_CC.2.5.1.1"/>
        /// </summary>
        /// <returns></returns>
        private static HashSet<FinalStateRequirementDetail> GetRequirements()
        {
            var map = new HashSet<FinalStateRequirementDetail>
            {
                new FinalStateRequirementDetail(DicomTag.TransactionUID, FinalStateRequirementCode.O),

                // SOP Common Module

                // Refer: https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.12.html#sect_C.12.1.1.2
                new FinalStateRequirementDetail(DicomTag.SpecificCharacterSet, FinalStateRequirementCode.RC),
                new FinalStateRequirementDetail(DicomTag.SOPClassUID, FinalStateRequirementCode.R),
                new FinalStateRequirementDetail(DicomTag.SOPInstanceUID, FinalStateRequirementCode.R),

                // Unified Procedure Step Scheduled Procedure Information Module
                new FinalStateRequirementDetail(DicomTag.ScheduledProcedureStepPriority, FinalStateRequirementCode.R),
                new FinalStateRequirementDetail(DicomTag.ScheduledProcedureStepModificationDateTime, FinalStateRequirementCode.R),
                new FinalStateRequirementDetail(DicomTag.ScheduledProcedureStepStartDateTime, FinalStateRequirementCode.R),
                new FinalStateRequirementDetail(DicomTag.InputReadinessState, FinalStateRequirementCode.R),

                // Unified Procedure Step Relationship Module

                // Patient Demographic Module

                // Patient Medical Module

                // Visit Identification Module

                // Visit Status Module

                // Visit Admission Module

                // Unified Procedure Step Progress Information Module
                new FinalStateRequirementDetail(DicomTag.ProcedureStepState, FinalStateRequirementCode.R),
                new FinalStateRequirementDetail(DicomTag.ProcedureStepProgressInformationSequence, FinalStateRequirementCode.X, new HashSet<FinalStateRequirementDetail>
                    {
                        new FinalStateRequirementDetail(DicomTag.ProcedureStepCancellationDateTime, FinalStateRequirementCode.X),
                        new FinalStateRequirementDetail(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, FinalStateRequirementCode.X),
                    }),

                // Unified Procedure Step Performed Procedure Information Module
                new FinalStateRequirementDetail(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, FinalStateRequirementCode.P, new HashSet<FinalStateRequirementDetail>
                    {
                        new FinalStateRequirementDetail(DicomTag.ActualHumanPerformersSequence, FinalStateRequirementCode.RC, new HashSet<FinalStateRequirementDetail>
                            {
                                new FinalStateRequirementDetail(DicomTag.HumanPerformerCodeSequence, FinalStateRequirementCode.RC),
                                new FinalStateRequirementDetail(DicomTag.HumanPerformerName, FinalStateRequirementCode.RC),
                            }),
                        new FinalStateRequirementDetail(DicomTag.PerformedStationNameCodeSequence, FinalStateRequirementCode.P),
                        new FinalStateRequirementDetail(DicomTag.PerformedProcedureStepStartDateTime, FinalStateRequirementCode.P),
                        new FinalStateRequirementDetail(DicomTag.PerformedWorkitemCodeSequence, FinalStateRequirementCode.P),
                        new FinalStateRequirementDetail(DicomTag.PerformedProcedureStepEndDateTime, FinalStateRequirementCode.P),
                        new FinalStateRequirementDetail(DicomTag.OutputInformationSequence, FinalStateRequirementCode.P),
                    }),
            };

            return map;
        }
    }
}
