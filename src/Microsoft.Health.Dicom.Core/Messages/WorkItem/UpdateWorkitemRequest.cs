// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class UpdateWorkitemRequest : IRequest<UpdateWorkitemResponse>
{
    public UpdateWorkitemRequest(DicomDataset dicomDataset, string requestContentType, string workitemInstanceUid, string transactionUid)
    {
        DicomDataset = dicomDataset;
        RequestContentType = requestContentType;
        WorkitemInstanceUid = workitemInstanceUid;
        TransactionUid = transactionUid;
    }

    public string WorkitemInstanceUid { get; }

    public string TransactionUid { get; }

    public DicomDataset DicomDataset { get; }

    public string RequestContentType { get; }
}
