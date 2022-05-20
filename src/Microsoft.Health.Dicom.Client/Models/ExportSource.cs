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
/// A collection of <see langword="static"/> utilities for creating export source options
/// that specify what data should be copied.
/// </summary>
public static class ExportSource
{
    /// <summary>
    /// Creates export source options for a list of DICOM identifiers.
    /// </summary>
    /// <param name="identifiers">One or more identifiers.</param>
    /// <returns>The corresponding export source options.</returns>
    /// <exception cref="ArgumentException"><paramref name="identifiers"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="identifiers"/> is <see langword="null"/>.</exception>
    public static ExportDataOptions<ExportSourceType> ForIdentifiers(params DicomIdentifier[] identifiers)
    {
        EnsureArg.HasItems(identifiers, nameof(identifiers));
        return new ExportDataOptions<ExportSourceType>(
            ExportSourceType.Identifiers,
            new IdentifierExportOptions { Values = identifiers });
    }

    /// <summary>
    /// Creates export source options for a list of DICOM identifiers.
    /// </summary>
    /// <param name="identifiers">One or more identifiers.</param>
    /// <returns>The corresponding export source options.</returns>
    /// <exception cref="ArgumentException"><paramref name="identifiers"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="identifiers"/> is <see langword="null"/>.</exception>
    public static ExportDataOptions<ExportSourceType> ForIdentifiers(IReadOnlyCollection<DicomIdentifier> identifiers)
    {
        EnsureArg.HasItems(identifiers, nameof(identifiers));
        return new ExportDataOptions<ExportSourceType>(
            ExportSourceType.Identifiers,
            new IdentifierExportOptions { Values = identifiers });
    }
}
