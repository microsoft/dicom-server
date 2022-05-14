// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models.Export;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Represents the set of data to be copied by an export operation.
/// </summary>
public sealed class ExportSource
{
    /// <summary>
    /// Gets the type of source this instance represents.
    /// </summary>
    /// <value>A type denoting the kind of source.</value>
    public ExportSourceType Type { get; }

    internal object Configuration { get; }

    private ExportSource(ExportSourceType type, object configuration)
    {
        Type = type;
        Configuration = EnsureArg.IsNotNull(configuration, nameof(configuration));
    }

    /// <summary>
    /// Creates an export source for a list of DICOM identifiers.
    /// </summary>
    /// <param name="identifiers">One or more identifiers.</param>
    /// <returns>The corresponding export source value.</returns>
    /// <exception cref="ArgumentException"><paramref name="identifiers"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="identifiers"/> is <see langword="null"/>.</exception>
    public static ExportSource ForIdentifiers(params DicomIdentifier[] identifiers)
    {
        EnsureArg.HasItems(identifiers, nameof(identifiers));
        return new ExportSource(ExportSourceType.Identifiers, new IdentifierExportOptions { Values = identifiers });
    }

    /// <summary>
    /// Creates an export source for a list of DICOM identifiers.
    /// </summary>
    /// <param name="identifiers">One or more identifiers.</param>
    /// <returns>The corresponding export source value.</returns>
    /// <exception cref="ArgumentException"><paramref name="identifiers"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="identifiers"/> is <see langword="null"/>.</exception>
    public static ExportSource ForIdentifiers(IReadOnlyCollection<DicomIdentifier> identifiers)
    {
        EnsureArg.HasItems(identifiers, nameof(identifiers));
        return new ExportSource(ExportSourceType.Identifiers, new IdentifierExportOptions { Values = identifiers });
    }
}
