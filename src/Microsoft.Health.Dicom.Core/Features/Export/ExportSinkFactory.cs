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
    /// Asynchronously creates a new instance of the <see cref="IExportSink"/> interface whose implementation
    /// is based on given <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The configuration for a specific sink type.</param>
    /// <param name="operationId">The ID for the export operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="ValidateAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the corresponding
    /// <see cref="IExportSink"/> instance
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="TypedConfiguration{T}.Type"/> property.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    public Task<IExportSink> CreateSinkAsync(TypedConfiguration<ExportDestinationType> destination, Guid operationId, CancellationToken cancellationToken = default)
        => GetProvider(EnsureArg.IsNotNull(destination, nameof(destination)).Type)
            .CreateSinkAsync(_serviceProvider, destination.Configuration, operationId, cancellationToken);

    /// <summary>
    /// Asynchronously ensures that the given configuration can be used to create a valid sink.
    /// </summary>
    /// <remarks>
    /// Based on the implementation, this method may also modify the values of the configuration.
    /// For example, it may help provide sink-specific security measures for sensitive settings.
    /// </remarks>
    /// <param name="destination">The configuration for a specific sink type.</param>
    /// <param name="operationId">The ID for the export operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="ValidateAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the validated <paramref name="destination"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">
    /// There is no provider configured for the value of the <see cref="TypedConfiguration{T}.Type"/> property.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    /// <exception cref="ValidationException">There were one or more problems with the sink-specific configuration.</exception>
    public async Task<TypedConfiguration<ExportDestinationType>> ValidateAsync(TypedConfiguration<ExportDestinationType> destination, Guid operationId, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(destination, nameof(destination));

        IExportSinkProvider provider = GetProvider(destination.Type);
        return new TypedConfiguration<ExportDestinationType>
        {
            Configuration = await provider.ValidateAsync(destination.Configuration, operationId, cancellationToken),
            Type = destination.Type,
        };
    }

    private IExportSinkProvider GetProvider(ExportDestinationType type)
    {
        if (!_providers.TryGetValue(type, out IExportSinkProvider provider))
            throw new KeyNotFoundException(
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.UnsupportedExportDestination, type));

        return provider;
    }
}
