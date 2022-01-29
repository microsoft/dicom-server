// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Messages.WorkitemMessages
{
    public sealed class AddWorkitemResponse
    {
        public AddWorkitemResponse(WorkitemResponseStatus status, Uri uri, string message = null)
        {
            Status = status;
            Uri = uri;
            Message = message;
        }

        public WorkitemResponseStatus Status { get; }

        public Uri Uri { get; }

        public string Message { get; }
    }
}
