// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class UpdateWorkitemResponse
{
    public UpdateWorkitemResponse(WorkitemResponseStatus status, Uri uri, string message = null, bool hasWarningOrFailure = false)
    {
        Status = status;
        Uri = uri;
        Message = message;
        HasWarningOrFailure = hasWarningOrFailure;
    }

    public WorkitemResponseStatus Status { get; }

    public Uri Uri { get; }

    public string Message { get; }

    public bool HasWarningOrFailure { get; }
}
