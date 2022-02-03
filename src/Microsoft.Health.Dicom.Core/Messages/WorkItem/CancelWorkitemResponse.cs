// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Messages.Workitem
{
    public sealed class CancelWorkitemResponse
    {
        public CancelWorkitemResponse(WorkitemResponseStatus status, string message = null)
        {
            Status = status;
            Message = message;
        }

        public WorkitemResponseStatus Status { get; }

        public string Message { get; }
    }
}
