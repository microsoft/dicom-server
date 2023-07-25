// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a provider of <see cref="IExportSource"/> instances indicated by the value
/// of the <see cref="Type"/> property.
/// </summary>
public interface IExportSourceProvider
{
    /// <summary>
    /// Gets the type of the source produced by this provider.
    /// </summary>
    /// <value>A value that represents the type of associated <see cref="IExportSource"/> instances.</value>
    ExportSourceType Type { get; }

    /// <summary>
    /// Asynchronously creates a new instance of the <see cref="IExportSource"/> interface whose implementation
    /// is based on the value of the <see cref="Type"/> property.
    /// </summary>
    /// <param name="options">The source-specific options.</param>
    /// <param name="partition">The data partition.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="ValidateAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is the corresponding
    /// instance of the <see cref="IExportSource"/> interface.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="partition"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IExportSource> CreateAsync(object options, Partition partition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously ensures that the given <paramref name="options"/> can be used to create a valid source.
    /// </summary>
    /// <param name="options">The source-specific options.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the <see cref="ValidateAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    /// <exception cref="ValidationException">There were one or more problems with the <paramref name="options"/>.</exception>
    Task ValidateAsync(object options, CancellationToken cancellationToken = default);
}
