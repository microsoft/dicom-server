// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a factory that creates <see cref="IExportSink"/> instances based on the configured providers.
/// </summary>
public sealed class ExportSinkFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ExportDestinationType, IExportSinkProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportSinkFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> to retrieve additional dependencies per provider.</param>
    /// <param name="providers">A collection of sink providers.</param>
    /// <exception cref="ArgumentException">
    /// Two or more providers have the same value for their <see cref="IExportSinkProvider.Type"/> property.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="serviceProvider"/> or <paramref name="providers"/> is <see langword="null"/>.
    /// </exception>
    public ExportSinkFactory(IServiceProvider serviceProvider, IEnumerable<IExportSinkProvider> providers)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _providers = EnsureArg.IsNotNull(providers, nameof(providers)).ToDictionary(x => x.Type);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IExportSink"/> interface whose implementation
    /// is based on given <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The configuration for a specific destination type.</param>
    /// <param name="operationId">The ID for the export operation.</param>
    /// <returns>The corresponding <see cref="IExportSink"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="TypedConfiguration{T}.Type"/> property.
    /// </exception>
    public IExportSink CreateSink(TypedConfiguration<ExportDestinationType> destination, Guid operationId)
        => GetProvider(EnsureArg.IsNotNull(destination, nameof(destination)).Type)
            .Create(_serviceProvider, destination.Configuration, operationId);

    /// <summary>
    /// Ensures that the given configuration can be used to create a valid sink.
    /// </summary>
    /// <remarks>
    /// Based on the implementation, this method may also modify the values of the <paramref name="destination"/>.
    /// For example, it may help provide sink-specific security measures for sensitive settings.
    /// </remarks>
    /// <param name="destination">The configuration for a specific destination type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="TypedConfiguration{T}.Type"/> property.
    /// </exception>
    public void Validate(TypedConfiguration<ExportDestinationType> destination)
        => GetProvider(EnsureArg.IsNotNull(destination, nameof(destination)).Type)
            .Validate(destination.Configuration);

    private IExportSinkProvider GetProvider(ExportDestinationType type)
    {
        if (!_providers.TryGetValue(type, out IExportSinkProvider provider))
            throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnsupportedExportDestination, type));

        return provider;
    }
}
