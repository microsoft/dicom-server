// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryResultEntry
    {
        public QueryResultEntry(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long version)
        {
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
