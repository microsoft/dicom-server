// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.CosmosDb.Config
{
    public class DicomCosmosConfiguration
    {
        /// <summary>
        /// Gets the DICOM tags that should be indexed and made queryable.
        /// The StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID will be indexed automatically.
        /// TODO: We should validate that the attributes defined here have a sensible value representation.
        /// </summary>
        public HashSet<DicomAttributeId> QueryAttributes { get; } = new HashSet<DicomAttributeId>()
        {
            new DicomAttributeId(DicomTag.PatientName),
            new DicomAttributeId(DicomTag.PatientID),
            new DicomAttributeId(DicomTag.AccessionNumber),
            new DicomAttributeId(DicomTag.Modality),
            new DicomAttributeId(DicomTag.ReferringPhysicianName),
            new DicomAttributeId(DicomTag.StudyDate),
        };
    }
}
