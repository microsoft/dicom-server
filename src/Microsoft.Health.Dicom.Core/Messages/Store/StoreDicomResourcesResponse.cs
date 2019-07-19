// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public sealed class StoreDicomResourcesResponse : BaseStatusCodeResponse
    {
        public StoreDicomResourcesResponse(HttpStatusCode statusCode)
            : base((int)statusCode)
        {
        }

        public StoreDicomResourcesResponse(HttpStatusCode statusCode, DicomDataset responseDataset)
            : base((int)statusCode)
        {
            EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));

            Dataset = responseDataset;
        }

        public DicomDataset Dataset { get; }
    }
}
