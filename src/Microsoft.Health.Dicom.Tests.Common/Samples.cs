// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class Samples
    {
        public static DicomFile CreateRandomDicomFile(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null)
        {
            return new DicomFile(CreateRandomInstanceDataset(studyInstanceUID, seriesInstanceUID, sopInstanceUID));
        }

        public static DicomDataset CreateRandomInstanceDataset(
            string studyInstanceUID = null,
            string seriesInstanceUID = null,
            string sopInstanceUID = null,
            string sopClassUID = null)
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, sopInstanceUID ?? Guid.NewGuid().ToString() },
                { DicomTag.SOPClassUID, sopClassUID ?? Guid.NewGuid().ToString() },
            };
        }
    }
}
