// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Update;

public class UpdateInstanceResponse
{
    public UpdateInstanceResponse(OperationReference operationReference)
    {
        Operation = EnsureArg.IsNotNull(operationReference);
    }

    public OperationReference Operation { get; }
}
