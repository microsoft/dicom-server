// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Metadata on FileProperty table in database
/// </summary>
public readonly struct IndexedFileProperties : IEquatable<IndexedFileProperties>
{
    /// <summary>
    /// Total indexed FileProperty in database
    /// </summary>
    public long TotalIndexed { get; init; }

    /// <summary>
    /// Total sum of all ContentLength rows in FileProperty table
    /// </summary>
    public long TotalSum { get; init; }

    public override bool Equals(object obj) => obj is IndexedFileProperties other && Equals(other);

    public bool Equals(IndexedFileProperties other)
        => TotalIndexed == other.TotalIndexed && TotalSum == other.TotalSum;

    public override int GetHashCode()
        => HashCode.Combine(TotalIndexed, TotalSum);

    public static bool operator ==(IndexedFileProperties left, IndexedFileProperties right)
        => left.Equals(right);

    public static bool operator !=(IndexedFileProperties left, IndexedFileProperties right)
        => !(left == right);
}
