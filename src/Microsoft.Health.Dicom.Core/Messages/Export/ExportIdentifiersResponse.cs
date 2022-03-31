// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Export;

public class ExportIdentifiersResponse
{
    public ExportIdentifiersResponse(OperationReference operationReference)
        => Operation = EnsureArg.IsNotNull(operationReference);

    public OperationReference Operation { get; }
}
