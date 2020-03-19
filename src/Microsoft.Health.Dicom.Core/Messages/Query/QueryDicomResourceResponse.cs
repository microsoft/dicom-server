// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public sealed class QueryDicomResourceResponse
    {
        public QueryDicomResourceResponse(IEnumerable<DicomDataset> responseDataset = null)
        {
            ResponseDataset = responseDataset ?? Enumerable.Empty<DicomDataset>();
        }

        public IEnumerable<DicomDataset> ResponseDataset { get; }
    }
}
