// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class ChangeWorkitemStateRequest : IRequest<ChangeWorkitemStateResponse>
{
    public ChangeWorkitemStateRequest(DicomDataset dicomDataset, string requestContentType, string workitemInstanceUid)
    {
        DicomDataset = dicomDataset;
        RequestContentType = requestContentType;
        WorkitemInstanceUid = workitemInstanceUid;
    }

    public DicomDataset DicomDataset { get; }
    public string RequestContentType { get; }
    public string WorkitemInstanceUid { get; }
}
