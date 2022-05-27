// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class ChangeWorkitemStateRequest : IRequest<ChangeWorkitemStateResponse>
{
    public ChangeWorkitemStateRequest(Stream requestBody, string requestContentType, string workitemInstanceUid)
    {
        RequestBody = requestBody;
        RequestContentType = requestContentType;
        WorkitemInstanceUid = workitemInstanceUid;
    }

    public Stream RequestBody { get; }
    public string RequestContentType { get; }
    public string WorkitemInstanceUid { get; }
}
