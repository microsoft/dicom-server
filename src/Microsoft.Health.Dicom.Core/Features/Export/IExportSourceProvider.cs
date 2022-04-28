// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Partition;
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
    /// Creates a new instance of the <see cref="IExportSource"/> interface whose implementation
    /// is based on the value of the <see cref="Type"/> property.
    /// </summary>
    /// <param name="provider">An <see cref="IServiceProvider"/> to retrieve additional dependencies.</param>
    /// <param name="config">The source-specific configuration.</param>
    /// <param name="partition">The data partition.</param>
    /// <returns>The corresponding instance of the <see cref="IExportSource"/> interface.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider"/>, <paramref name="config"/>, or <paramref name="partition"/> is <see langword="null"/>.
    /// </exception>
    IExportSource Create(IServiceProvider provider, IConfiguration config, PartitionEntry partition);

    /// <summary>
    /// Ensures that the given <paramref name="config"/> can be used to create a valid source.
    /// </summary>
    /// <param name="config">The source-specific configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    /// <exception cref="ValidationException">There were one or more problems with the <paramref name="config"/>.</exception>
    void Validate(IConfiguration config);
}
