// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public sealed class QueryResourceResponse
    {
        public QueryResourceResponse(IEnumerable<DicomDataset> responseDataset, IReadOnlyCollection<string> erroneousTags)
        {
            ResponseDataset = EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));
            ErroneousTags = EnsureArg.IsNotNull(erroneousTags, nameof(erroneousTags));
        }

        public IEnumerable<DicomDataset> ResponseDataset { get; }

        public IReadOnlyCollection<string> ErroneousTags { get; }
    }
}
