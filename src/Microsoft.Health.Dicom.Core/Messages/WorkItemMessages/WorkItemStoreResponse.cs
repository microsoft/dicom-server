// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Messages.WorkitemMessages
{
    public sealed class WorkitemStoreResponse
    {
        public WorkitemStoreResponse(WorkitemStoreResponseStatus status, Workitem workItem)
        {
            Status = status;
            Workitem = workItem;
        }

        public WorkitemStoreResponseStatus Status { get; }

        public Workitem Workitem { get; }
    }
}
