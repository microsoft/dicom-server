// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Export;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents the Azure Durable Function that perform the export of data from Azure Health Data Services to a pre-defined sink.
/// </summary>
public partial class ExportDurableFunction
{
    private readonly ExportSourceFactory _sourceFactory;
    private readonly ExportSinkFactory _sinkFactory;
    private readonly ExportOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDurableFunction"/> class.
    /// </summary>
    /// <param name="sourceFactory">A factory for creating <see cref="IExportSource"/> instances.</param>
    /// <param name="sinkFactory">A factory for creating <see cref="IExportSink"/> instances.</param>
    /// <param name="options">A collection of settings related to the execution of export operations.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="sourceFactory"/>, <paramref name="sinkFactory"/>, or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public ExportDurableFunction(ExportSourceFactory sourceFactory, ExportSinkFactory sinkFactory, IOptions<ExportOptions> options)
    {
        _sourceFactory = EnsureArg.IsNotNull(sourceFactory, nameof(sinkFactory));
        _sinkFactory = EnsureArg.IsNotNull(sinkFactory, nameof(sinkFactory));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
    }
}
