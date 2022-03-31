// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Messages.Export;

public class ExportIdentifiersRequest : IRequest<ExportIdentifiersResponse>
{
    public ExportIdentifiersRequest(ExportIdentifiersInput input)
        => Input = EnsureArg.IsNotNull(input, nameof(input));

    public ExportIdentifiersInput Input { get; }
}
