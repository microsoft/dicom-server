// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public sealed class DicomStoreResponse
    {
        public DicomStoreResponse(DicomStoreResponseStatus status, DicomDataset responseDataset)
        {
            Status = status;
            Dataset = responseDataset;
        }

        public DicomStoreResponseStatus Status { get; }

        public DicomDataset Dataset { get; }
    }
}
