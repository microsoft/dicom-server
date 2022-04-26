// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class RetrieveWorkitemResponse
{
    public RetrieveWorkitemResponse(WorkitemResponseStatus status, DicomDataset responseDataset)
    {
        Dataset = EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));
        Status = status;
    }

    public DicomDataset Dataset { get; }
    public WorkitemResponseStatus Status { get; }
}
