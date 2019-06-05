// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public class StoreDicomResourcesResponse : BaseStatusCodeResponse
    {
        public StoreDicomResourcesResponse(HttpStatusCode statusCode, DicomDataset responseDataset)
            : base(statusCode)
        {
            EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));

            ResponseDataset = responseDataset;
        }

        public StoreDicomResourcesResponse(HttpStatusCode statusCode)
            : base(statusCode)
        {
        }

        public DicomDataset ResponseDataset { get; }
    }
}
