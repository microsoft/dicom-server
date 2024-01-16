// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model;

public class StudyResult
{
    public string StudyInstanceUid { get; init; }
    public string PatientId { get; init; }
    public string PatientName { get; init; }
    public string ReferringPhysicianName { get; init; }
    public DateTime? StudyDate { get; init; }
    public string StudyDescription { get; init; }
    public string AccessionNumber { get; init; }
    public DateTime? PatientBirthDate { get; init; }
#pragma warning disable CA1819 // Properties should not return arrays
    public string[] ModalitiesInStudy { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays
    public int NumberofStudyRelatedInstances { get; init; }

    private DicomDataset _dicomDataset;
    public DicomDataset DicomDataset
    {
        get
        {
            if (_dicomDataset == null)
            {
                _dicomDataset = new DicomDataset()
                {
                    { DicomTag.StudyInstanceUID, StudyInstanceUid },
                    { DicomTag.NumberOfStudyRelatedInstances, NumberofStudyRelatedInstances },
                };

                _dicomDataset.AddValueIfNotNull(DicomTag.PatientID, PatientId);
                _dicomDataset.AddValueIfNotNull(DicomTag.PatientName, PatientName);
                _dicomDataset.AddValueIfNotNull(DicomTag.ReferringPhysicianName, ReferringPhysicianName);
                _dicomDataset.AddValueIfNotNull(DicomTag.StudyDescription, StudyDescription);
                _dicomDataset.AddValueIfNotNull(DicomTag.AccessionNumber, AccessionNumber);

                if (ModalitiesInStudy?.Length > 0)
                {
                    _dicomDataset.Add(DicomTag.ModalitiesInStudy, ModalitiesInStudy);
                }
                if (StudyDate.HasValue)
                {
                    _dicomDataset.Add(DicomTag.StudyDate, StudyDate.Value);
                }
                if (PatientBirthDate.HasValue)
                {
                    _dicomDataset.Add(DicomTag.PatientBirthDate, PatientBirthDate.Value);
                }
            }
            return _dicomDataset;
        }
    }

    public static readonly ImmutableHashSet<DicomTag> AvailableTags = ImmutableHashSet.Create<DicomTag>
    (
        DicomTag.StudyInstanceUID,
        DicomTag.PatientID,
        DicomTag.PatientName,
        DicomTag.ReferringPhysicianName,
        DicomTag.StudyDate,
        DicomTag.StudyDescription,
        DicomTag.AccessionNumber,
        DicomTag.PatientBirthDate,
        DicomTag.ModalitiesInStudy,
        DicomTag.NumberOfStudyRelatedInstances
    );
}
