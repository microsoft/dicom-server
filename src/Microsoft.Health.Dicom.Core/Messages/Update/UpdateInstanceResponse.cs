// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Update;

public class UpdateInstanceResponse
{
    public UpdateInstanceResponse(OperationReference operationReference, int statusCode)
    {
        Operation = EnsureArg.IsNotNull(operationReference);
        StatusCode = statusCode;
    }

    public UpdateInstanceResponse(DicomDataset dataset, int statusCode)
    {
        FailedDataset = EnsureArg.IsNotNull(dataset);
        StatusCode = statusCode;
    }

    public OperationReference Operation { get; }

    public DicomDataset FailedDataset { get; }

    public int StatusCode { get; }
}
