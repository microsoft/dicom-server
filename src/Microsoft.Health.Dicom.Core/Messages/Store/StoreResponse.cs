// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public sealed class StoreResponse
    {
        public StoreResponse(StoreResponseStatus status, DicomDataset responseDataset)
        {
            Status = status;
            Dataset = responseDataset;
        }

        public StoreResponseStatus Status { get; }

        public DicomDataset Dataset { get; }
    }
}
