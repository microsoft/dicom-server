// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class UpdateWorkitemRequest : IRequest<UpdateWorkitemResponse>
{
    public UpdateWorkitemRequest(Stream requestBody, string requestContentType, string workitemInstanceUid, string transactionUid)
    {
        RequestBody = requestBody;
        RequestContentType = requestContentType;
        WorkitemInstanceUid = workitemInstanceUid;
        TransactionUid = transactionUid;
    }

    public string WorkitemInstanceUid { get; }

    public string TransactionUid { get; }

    public Stream RequestBody { get; }

    public string RequestContentType { get; }
}
