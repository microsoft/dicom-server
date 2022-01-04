// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Messages.WorkitemMessages
{
    public sealed class WorkitemStoreResponse
    {
        public WorkitemStoreResponse(WorkitemStoreResponseStatus status, DicomDataset dataset)
        {
            Status = status;
            Dataset = dataset;
        }

        public WorkitemStoreResponseStatus Status { get; }

        public DicomDataset Dataset { get; }
    }
}
