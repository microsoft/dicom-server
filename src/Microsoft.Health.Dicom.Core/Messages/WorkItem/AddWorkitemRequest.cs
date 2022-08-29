// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public class AddWorkitemRequest : IRequest<AddWorkitemResponse>
{
    public AddWorkitemRequest(
        DicomDataset dicomDataset,
        string requestContentType,
        string workItemInstanceUid)
    {
        WorkitemInstanceUid = workItemInstanceUid;
        DicomDataset = dicomDataset;
        RequestContentType = requestContentType;
    }

    public string WorkitemInstanceUid { get; }

    public DicomDataset DicomDataset { get; }

    public string RequestContentType { get; }
}
