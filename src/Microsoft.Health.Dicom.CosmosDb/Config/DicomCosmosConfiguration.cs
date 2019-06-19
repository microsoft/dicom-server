// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.CosmosDb.Config
{
    public class DicomCosmosConfiguration
    {
        /// <summary>
        /// Gets the DICOM tags that should be indexed and made queryable.
        /// The StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID will be indexed automatically.
        /// </summary>
        public IEnumerable<DicomTag> QueryAttributes { get; } = new[]
        {
            DicomTag.PatientName,
            DicomTag.PatientID,
            DicomTag.AccessionNumber,
            DicomTag.Modality,
            DicomTag.ReferringPhysicianName,
            DicomTag.StudyDate,
        };
    }
}
