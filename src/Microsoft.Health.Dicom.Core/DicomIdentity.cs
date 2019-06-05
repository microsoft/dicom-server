// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core
{
    public class DicomIdentity
    {
        public DicomIdentity(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            SopInstanceUID = dicomDataset.GetSingleValueOrDefault<string>(DicomTag.SOPInstanceUID, null);
            SeriesInstanceUID = dicomDataset.GetSingleValueOrDefault<string>(DicomTag.SeriesInstanceUID, null);
            StudyInstanceUID = dicomDataset.GetSingleValueOrDefault<string>(DicomTag.StudyInstanceUID, null);
            SopClassUID = dicomDataset.GetSingleValueOrDefault<string>(DicomTag.SOPClassUID, null);
        }

        public string SopClassUID { get; }

        public string SopInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string StudyInstanceUID { get; }
    }
}
