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
        public IEnumerable<DicomTag> QueryAttributes { get; } = new[] { DicomTag.PatientName, DicomTag.ReferringPhysicianName, DicomTag.Modality };
    }
}
