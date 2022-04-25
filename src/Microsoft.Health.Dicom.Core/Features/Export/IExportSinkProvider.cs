// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a provider of <see cref="IExportSink"/> instances indicated by the value
/// of the <see cref="Type"/> property.
/// </summary>
public interface IExportSinkProvider
{
    /// <summary>
    /// Gets the type of the sink produced by this provider.
    /// </summary>
    /// <value>A value that represents the type of associated <see cref="IExportSink"/> instances.</value>
    ExportDestinationType Type { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="IExportSink"/> interface whose implementation
    /// is based on the value of the <see cref="Type"/> property.
    /// </summary>
    /// <param name="provider">An <see cref="IServiceProvider"/> to retrieve additional dependencies.</param>
    /// <param name="config">The sink-specific configuration.</param>
    /// <param name="operationId">The ID for the export operation.</param>
    /// <returns>The corresponding instance of the <see cref="IExportSink"/> interface.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider"/> or <paramref name="config"/> is <see langword="null"/>.
    /// </exception>
    IExportSink Create(IServiceProvider provider, IConfiguration config, Guid operationId);

    /// <summary>
    /// Ensures that the given <paramref name="config"/> can be used to create a valid sink.
    /// </summary>
    /// <remarks>
    /// Based on the implementation, this method may also modify the values of the <paramref name="config"/>.
    /// For example, it may help provide sink-specific security measures for sensitive settings.
    /// </remarks>
    /// <param name="config">The sink-specific configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    void Validate(IConfiguration config);
}
