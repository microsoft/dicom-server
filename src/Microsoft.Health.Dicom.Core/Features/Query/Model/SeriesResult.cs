// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model;

public class SeriesResult
{
    public string StudyInstanceUid { get; init; }
    public string SeriesInstanceUid { get; init; }
    public string Modality { get; init; }
    public DateTime? PerformedProcedureStepStartDate { get; init; }
    public string ManufacturerModelName { get; init; }
    public int NumberOfSeriesRelatedInstances { get; init; }

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
                    { DicomTag.SeriesInstanceUID, SeriesInstanceUid },
                    { DicomTag.NumberOfSeriesRelatedInstances, NumberOfSeriesRelatedInstances }
                };
                _dicomDataset.AddValueIfNotNull(DicomTag.Modality, Modality);
                _dicomDataset.AddValueIfNotNull(DicomTag.ManufacturerModelName, ManufacturerModelName);
                if (PerformedProcedureStepStartDate.HasValue)
                {
                    _dicomDataset.Add(DicomTag.PerformedProcedureStepStartDate, PerformedProcedureStepStartDate.Value);
                }
            }
            return _dicomDataset;
        }
    }

    public static readonly ImmutableHashSet<DicomTag> AvailableTags = ImmutableHashSet.Create<DicomTag>
    (
        DicomTag.SeriesInstanceUID,
        DicomTag.Modality,
        DicomTag.PerformedProcedureStepStartDate,
        DicomTag.ManufacturerModelName,
        DicomTag.NumberOfSeriesRelatedInstances
    );
}
