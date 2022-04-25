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
/// Represents a factory that creates <see cref="IExportSource"/> instances based on the configured providers.
/// </summary>
public sealed class ExportSourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ExportSourceType, IExportSourceProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportSourceFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> to retrieve additional dependencies per provider.</param>
    /// <param name="providers">A collection of source providers.</param>
    /// <exception cref="ArgumentException">
    /// Two or more providers have the same value for their <see cref="IExportSourceProvider.Type"/> property.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="serviceProvider"/> or <paramref name="providers"/> is <see langword="null"/>.
    /// </exception>
    public ExportSourceFactory(IServiceProvider serviceProvider, IEnumerable<IExportSourceProvider> providers)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _providers = EnsureArg.IsNotNull(providers, nameof(providers)).ToDictionary(x => x.Type);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IExportSource"/> interface whose implementation
    /// is based on given <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The configuration for a specific source type.</param>
    /// <returns>The corresponding <see cref="IExportSource"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="TypedConfiguration{T}.Type"/> property.
    /// </exception>
    public IExportSource CreateSource(TypedConfiguration<ExportSourceType> source)
        => GetProvider(EnsureArg.IsNotNull(source, nameof(source)).Type).Create(_serviceProvider, source.Configuration);

    /// <summary>
    /// Ensures that the given configuration can be used to create a valid source.
    /// </summary>
    /// <param name="source">The configuration for a specific source type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="TypedConfiguration{T}.Type"/> property.
    /// </exception>
    public void Validate(TypedConfiguration<ExportSourceType> source)
        => GetProvider(EnsureArg.IsNotNull(source, nameof(source)).Type).Validate(source.Configuration);

    private IExportSourceProvider GetProvider(ExportSourceType type)
    {
        if (!_providers.TryGetValue(type, out IExportSourceProvider provider))
            throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnsupportedExportSource, type));

        return provider;
    }
}
