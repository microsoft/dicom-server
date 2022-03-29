// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Export;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents the Azure Durable Function that perform the export of data from Azure Health Data Services to a pre-defined sink.
/// </summary>
public partial class ExportDurableFunction
{
    private readonly IExportSinkFactory _sinkFactory;
    private readonly ExportOptions _options;

    public ExportDurableFunction(IExportSinkFactory sinkFactory, IOptions<ExportOptions> options)
    {
        _sinkFactory = EnsureArg.IsNotNull(sinkFactory, nameof(sinkFactory));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
    }
}
