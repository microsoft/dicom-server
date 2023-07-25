// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a factory that creates <see cref="IExportSource"/> instances based on the configured providers.
/// </summary>
public sealed class ExportSourceFactory
{
    private readonly Dictionary<ExportSourceType, IExportSourceProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportSourceFactory"/> class.
    /// </summary>
    /// <param name="providers">A collection of source providers.</param>
    /// <exception cref="ArgumentException">
    /// Two or more providers have the same value for their <see cref="IExportSourceProvider.Type"/> property.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="providers"/> is <see langword="null"/>.</exception>
    public ExportSourceFactory(IEnumerable<IExportSourceProvider> providers)
        => _providers = EnsureArg.IsNotNull(providers, nameof(providers)).ToDictionary(x => x.Type);

    /// <summary>
    /// Asynchronously creates a new instance of the <see cref="IExportSource"/> interface whose implementation
    /// is based on given <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The options for a specific source type.</param>
    /// <param name="partition">The data partition.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="ValidateAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the corresponding
    /// <see cref="IExportSource"/> instance
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="partition"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="ExportDataOptions{T}.Type"/> property.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    public Task<IExportSource> CreateAsync(ExportDataOptions<ExportSourceType> source, Partition partition, CancellationToken cancellationToken = default)
        => GetProvider(EnsureArg.IsNotNull(source, nameof(source)).Type).CreateAsync(source.Settings, partition, cancellationToken);

    /// <summary>
    /// Asynchronously ensures that the given options can be used to create a valid source.
    /// </summary>
    /// <param name="source">The options for a specific source type.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the <see cref="ValidateAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="ExportDataOptions{T}.Type"/> property.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    /// <exception cref="ValidationException">There were one or more problems with the source-specific options.</exception>
    public async Task ValidateAsync(ExportDataOptions<ExportSourceType> source, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(source, nameof(source));
        await GetProvider(source.Type).ValidateAsync(source.Settings, cancellationToken);
    }

    private IExportSourceProvider GetProvider(ExportSourceType type)
        => _providers.TryGetValue(type, out IExportSourceProvider provider)
            ? provider
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnsupportedExportSource, type));
}
