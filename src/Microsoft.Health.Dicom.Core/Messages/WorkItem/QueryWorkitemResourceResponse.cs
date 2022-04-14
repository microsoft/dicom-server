// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem;

public sealed class QueryWorkitemResourceResponse
{
    public QueryWorkitemResourceResponse(IReadOnlyList<DicomDataset> responseDataset, WorkitemResponseStatus status)
    {
        ResponseDatasets = EnsureArg.IsNotNull(responseDataset, nameof(responseDataset));
        Status = status;
    }

    public WorkitemResponseStatus Status { get; }

    public IReadOnlyList<DicomDataset> ResponseDatasets { get; }
}
