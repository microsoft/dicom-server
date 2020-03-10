// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryResultEntry
    {
        public QueryResultEntry(
            string studyInstanceUID,
            string seriesInstanceUID,
            string sOPInstanceUID,
            long version)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            SOPInstanceUID = sOPInstanceUID;
            Version = version;
        }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string SOPInstanceUID { get; }

        public long Version { get; }
    }
}
