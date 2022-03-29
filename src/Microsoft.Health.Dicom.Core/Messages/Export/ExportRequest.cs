// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Messages.Export;

public class ExportRequest : IRequest<ExportResponse>
{
    public ExportRequest(ExportInput exportInput)
    {
        ExportInput = exportInput;
    }

    public ExportInput ExportInput { get; }
}
