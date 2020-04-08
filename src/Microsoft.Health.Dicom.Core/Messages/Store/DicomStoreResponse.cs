// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public sealed class DicomStoreResponse : BaseStatusCodeResponse
    {
        public DicomStoreResponse(HttpStatusCode statusCode)
            : base((int)statusCode)
        {
        }

        public DicomStoreResponse(HttpStatusCode statusCode, DicomDataset responseDataset)
            : base((int)statusCode)
        {
            Dataset = responseDataset;
        }

        public DicomDataset Dataset { get; }
    }
}
