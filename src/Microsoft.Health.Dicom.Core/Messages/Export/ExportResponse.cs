// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Messages.Export;

public class ExportResponse
{
    public OperationReference Operation { get; }

    public ExportResponse(OperationReference operation)
        => Operation = EnsureArg.IsNotNull(operation, nameof(operation));
}
