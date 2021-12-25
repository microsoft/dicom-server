// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Workitem;

namespace Microsoft.Health.Dicom.Core.Messages.WorkitemMessages
{
    public sealed class WorkitemStoreResponse
    {
        public WorkitemStoreResponse(WorkitemStoreResponseStatus status, WorkitemDataset workItem)
        {
            Status = status;
            Workitem = workItem;
        }

        public WorkitemStoreResponseStatus Status { get; }

        public WorkitemDataset Workitem { get; }
    }
}
