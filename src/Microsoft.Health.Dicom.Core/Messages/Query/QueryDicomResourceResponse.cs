// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public sealed class QueryDicomResourceResponse : BaseStatusCodeResponse
    {
        public QueryDicomResourceResponse(HttpStatusCode statusCode)
            : base((int)statusCode)
        {
        }

        public QueryDicomResourceResponse(HttpStatusCode statusCode, IEnumerable<DicomDataset> responseDataset)
            : base((int)statusCode)
        {
            EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));

            ResponseDataset = responseDataset;
        }

        public IEnumerable<DicomDataset> ResponseDataset { get; }
    }
}
