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
    public class QueryDicomResourcesResponse : BaseStatusCodeResponse
    {
        public QueryDicomResourcesResponse(HttpStatusCode statusCode, IList<string> warnings)
            : base((int)statusCode)
        {
            Warnings = warnings;
            HasWarning = warnings.Count > 0;
        }

        public QueryDicomResourcesResponse(HttpStatusCode statusCode, IEnumerable<DicomDataset> responseMetadata, IList<string> warnings)
            : this(statusCode, warnings)
        {
            EnsureArg.IsNotNull(responseMetadata, nameof(responseMetadata));
            ResponseMetadata = responseMetadata;
        }

        public bool HasWarning { get; }

        public IEnumerable<string> Warnings { get; }

        public IEnumerable<DicomDataset> ResponseMetadata { get; }
    }
}
