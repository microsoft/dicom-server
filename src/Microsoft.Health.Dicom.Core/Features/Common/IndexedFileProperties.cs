// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Metadata on FileProperty table in database
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Identifiers are not equatable.")]
public readonly struct IndexedFileProperties
{
    /// <summary>
    /// Total indexed FileProperty in database
    /// </summary>
    public int TotalIndexed { get; init; }

    /// <summary>
    /// Total sum of all ContentLength rows in FileProperty table
    /// </summary>
    public long TotalSum { get; init; }
}
