// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Messages.Export;

public sealed class ExportRequest : IRequest<ExportResponse>
{
    public ExportSpecification Specification { get; }

    public ExportRequest(ExportSpecification spec)
    {
        Specification = EnsureArg.IsNotNull(spec, nameof(spec));
    }
}
