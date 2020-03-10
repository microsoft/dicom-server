// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryResultEntry
    {
        public string StudyInstanceUID { get; set; }

        public string SeriesInstanceUID { get; set; }

        public string SOPInstanceUID { get; set; }

        public long Version { get; set; }
    }
}
