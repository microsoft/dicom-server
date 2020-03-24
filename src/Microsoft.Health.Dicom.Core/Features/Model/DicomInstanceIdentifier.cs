// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features
{
    public class DicomInstanceIdentifier
    {
        public DicomInstanceIdentifier(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long version)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));

            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Version = version;
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public long Version { get; }
    }
}
