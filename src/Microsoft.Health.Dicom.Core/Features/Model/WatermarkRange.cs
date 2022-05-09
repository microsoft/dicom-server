// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Model;

/// <summary>
/// Represents a range of DICOM instance watermarks.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct WatermarkRange : IEquatable<WatermarkRange>
{
    public WatermarkRange(long start, long end)
    {
        Start = EnsureArg.IsGte(start, 1, nameof(start));
        End = EnsureArg.IsGte(end, start, nameof(end));
    }

    /// <summary>
    /// Gets inclusive starting instance watermark.
    /// </summary>
    public long Start { get; }

    /// <summary>
    /// Gets inclusive ending instance watermark.
    /// </summary>
    public long End { get; }

    public override bool Equals(object obj)
        => obj is WatermarkRange other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Start, End);

    public static bool operator ==(WatermarkRange left, WatermarkRange right)
        => left.Equals(right);

    public static bool operator !=(WatermarkRange left, WatermarkRange right)
        => !(left == right);

    public bool Equals(WatermarkRange other)
        => Start == other.Start && End == other.End;

    public void Deconstruct(out long start, out long end)
    {
        start = Start;
        end = End;
    }
    public WatermarkRange Combine(WatermarkRange range)
    {
        if (Start > range.Start)
        {
            return range.Combine(this);
        }
        EnsureArg.Is(range.Start, End + 1, nameof(range.Start));
        return new WatermarkRange(Start, range.End);
    }

    public static WatermarkRange Combine(IReadOnlyList<WatermarkRange> batches)
    {
        EnsureArg.IsNotNull(batches, nameof(batches));
        EnsureArg.IsGt(batches.Count, 0, nameof(batches));
        WatermarkRange result = batches[0];
        for (int i = 1; i < batches.Count; i++)
        {
            result = result.Combine(batches[i]);
        }
        return result;
    }


    public override string ToString()
        => "[" + Start + ", " + End + "]";
}
