// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Represents options for either what data should be exported from the DICOM service,
/// or where that data should be copied.
/// </summary>
public sealed class ExportDataOptions<T>
{
    /// <summary>
    /// Creates a new instance of the <see cref="ExportDataOptions{T}"/> class
    /// with the given type.
    /// </summary>
    /// <param name="type">The type of options this new instance represents.</param>
    public ExportDataOptions(T type)
    {
        Type = type;
    }

    internal ExportDataOptions(T type, object settings)
    {
        Type = type;
        Settings = EnsureArg.IsNotNull(settings, nameof(settings));
    }

    /// <summary>
    /// Gets the type of options this instance represents.
    /// </summary>
    /// <value>A type denoting the kind of options.</value>
    public T Type { get; }

    internal object Settings { get; }
}
