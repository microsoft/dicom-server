// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

public static class WorkitemQueryResponseBuilder
{
    /// <summary>
    /// Workitem attributes with a return key type of 1 or 2 (including conditionals).
    /// <see href='https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.5-3'/>.
    /// </summary>
    public static readonly HashSet<DicomTag> RequiredReturnTags = new HashSet<DicomTag>
    {
        // SOP Common Module
        DicomTag.SpecificCharacterSet,
        DicomTag.SOPClassUID,
        DicomTag.SOPInstanceUID,

        // Unified Procedure Step Scheduled Procedure Information Module
        DicomTag.ScheduledProcedureStepPriority,
        DicomTag.ProcedureStepLabel,
        DicomTag.WorklistLabel,
        DicomTag.ScheduledProcessingParametersSequence,
        DicomTag.ScheduledStationNameCodeSequence,
        DicomTag.ScheduledStationClassCodeSequence,
        DicomTag.ScheduledStationGeographicLocationCodeSequence,
        DicomTag.ScheduledHumanPerformersSequence,
        DicomTag.ScheduledProcedureStepStartDateTime,
        DicomTag.ScheduledWorkitemCodeSequence,
        DicomTag.InputReadinessState,
        DicomTag.InputInformationSequence,
        DicomTag.StudyInstanceUID,

        // Unified Procedure Step Relationship Module
        DicomTag.PatientName,
        DicomTag.PatientID,

        // Issuer of Patient ID Macro
        DicomTag.IssuerOfPatientID,
        DicomTag.IssuerOfPatientIDQualifiersSequence,

        DicomTag.OtherPatientIDsSequence,
        DicomTag.PatientBirthDate,
        DicomTag.PatientSex,
        DicomTag.AdmissionID,
        DicomTag.IssuerOfAdmissionIDSequence,
        DicomTag.AdmittingDiagnosesDescription,
        DicomTag.AdmittingDiagnosesCodeSequence,
        DicomTag.ReferencedRequestSequence,

        // Patient Medical Module
        DicomTag.MedicalAlerts,
        DicomTag.PregnancyStatus,
        DicomTag.SpecialNeeds,

        // Unified Procedure Step Progress Information Module
        DicomTag.ProcedureStepState,
        DicomTag.ProcedureStepProgressInformationSequence,
    };

    /// <summary>
    /// Builds workitem query response
    /// </summary>
    /// <returns></returns>
    public static QueryWorkitemResourceResponse BuildWorkitemQueryResponse(IReadOnlyList<DicomDataset> datasets, BaseQueryExpression queryExpression)
    {
        var status = WorkitemResponseStatus.NoContent;

        if (datasets.Any(x => x == null))
        {
            status = WorkitemResponseStatus.PartialContent;
        }
        else if (!datasets.Any())
        {
            status = WorkitemResponseStatus.Success;
        }

        var workitemResponses = datasets.Where(x => x != null).Select(m => GenerateResponseDataset(m, queryExpression)).ToList();

        return new QueryWorkitemResourceResponse(workitemResponses, status);
    }

    /// <summary>
    /// Includes workitem attributes as specified in
    /// <see href='https://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_11.9.2'/>.
    /// </summary>
    private static DicomDataset GenerateResponseDataset(DicomDataset dicomDataset, BaseQueryExpression queryExpression)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(queryExpression, nameof(queryExpression));

        // Should never be returned
        dicomDataset = dicomDataset.Remove(DicomTag.TransactionUID);

        if (queryExpression.IncludeFields.All)
        {
            return dicomDataset;
        }

        var tagsToReturn = new HashSet<DicomTag>(RequiredReturnTags);

        foreach (DicomTag tag in queryExpression.IncludeFields.DicomTags)
        {
            tagsToReturn.Add(tag);
        }

        foreach (var cond in queryExpression.FilterConditions)
        {
            tagsToReturn.Add(cond.QueryTag.Tag);
        }

        dicomDataset.Remove(di => !tagsToReturn.Any(
            t => t.Group == di.Tag.Group &&
            t.Element == di.Tag.Element));

        return dicomDataset;
    }
}
