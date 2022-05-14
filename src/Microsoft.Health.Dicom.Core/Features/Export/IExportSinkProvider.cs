// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
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
    /// Asynchronously creates a new instance of the <see cref="IExportSink"/> interface whose implementation
    /// is based on the value of the <see cref="Type"/> property.
    /// </summary>
    /// <param name="provider">An <see cref="IServiceProvider"/> to retrieve additional dependencies.</param>
    /// <param name="config">The sink-specific configuration.</param>
    /// <param name="operationId">The ID for the export operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="ValidateAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the corresponding
    /// instance of the <see cref="IExportSink"/> interface.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider"/> or <paramref name="config"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IExportSink> CreateAsync(IServiceProvider provider, IConfiguration config, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously stores sensitive information in a secure format and returns the updated configuration.
    /// </summary>
    /// <remarks>
    /// It is the responsibility of the <see cref="CreateAsync"/> method to retrieve any sensitive information
    /// that was secured by this method.
    /// </remarks>
    /// <param name="config">The sink-specific configuration.</param>
    /// <param name="operationId">The ID for the export operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="SecureSensitiveInfoAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is a new configuration with any sensitive
    /// information secured.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IConfiguration> SecureSensitiveInfoAsync(IConfiguration config, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously ensures that the given <paramref name="config"/> can be used to create a valid sink.
    /// </summary>
    /// <param name="config">The sink-specific configuration.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the <see cref="ValidateAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    /// <exception cref="ValidationException">There were one or more problems with the <paramref name="config"/>.</exception>
    Task ValidateAsync(IConfiguration config, CancellationToken cancellationToken = default);
}
