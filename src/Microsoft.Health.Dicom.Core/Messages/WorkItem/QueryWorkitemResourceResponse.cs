// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem
{
    public sealed class QueryWorkitemResourceResponse
    {
        public QueryWorkitemResourceResponse(IEnumerable<DicomDataset> responseDataset)
        {
            ResponseDatasets = EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));
        }

        public IEnumerable<DicomDataset> ResponseDatasets { get; }
    }
}
