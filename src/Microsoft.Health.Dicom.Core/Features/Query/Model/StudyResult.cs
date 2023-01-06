// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;

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
    public string[] ModalitiesInStudy { get; init; }
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
                    { DicomTag.PatientID, PatientId },
                    { DicomTag.PatientName, PatientName },
                    { DicomTag.ReferringPhysicianName, ReferringPhysicianName },
                    { DicomTag.StudyDescription, StudyDescription },
                    { DicomTag.AccessionNumber, AccessionNumber },
                    { DicomTag.ModalitiesInStudy, ModalitiesInStudy },
                    { DicomTag.NumberOfStudyRelatedInstances, NumberofStudyRelatedInstances},
                };
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
}
