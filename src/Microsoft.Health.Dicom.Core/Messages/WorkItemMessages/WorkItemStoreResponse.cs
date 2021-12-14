// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Messages.WorkItemMessages
{
    public sealed class WorkItemStoreResponse
    {
        public WorkItemStoreResponse(WorkItemStoreResponseStatus status, WorkItem workItem)
        {
            Status = status;
            WorkItem = workItem;
        }

        public WorkItemStoreResponseStatus Status { get; }

        public WorkItem WorkItem { get; }
    }
}
