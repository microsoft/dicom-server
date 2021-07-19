// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Model
{
    /// <summary>
    /// Represents a range of DICOM instance watermarks.
    /// </summary>
    public readonly struct WatermarkRange : IEquatable<WatermarkRange>
    {
        public WatermarkRange(long start, long end)
        {
            Start = EnsureArg.IsGte(start, 0, nameof(start));
            End = EnsureArg.IsGte(end, 0, nameof(end));
        }

        /// <summary>
        /// Gets inclusive starting instance watermark.
        /// </summary>
        public long Start { get; }

        /// <summary>
        /// Gets exclusive ending instance watermark.
        /// </summary>
        public long End { get; }

        /// <summary>
        /// Gets the maximum number of instances within this range.
        /// </summary>
        /// <remarks>
        /// Some instances may be missing in the range due to previous deletion operations.
        /// </remarks>
        public long Count => End - Start;

        public override bool Equals(object obj)
            => obj is WatermarkRange other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Start, Count);

        public static bool operator ==(WatermarkRange left, WatermarkRange right)
            => left.Equals(right);

        public static bool operator !=(WatermarkRange left, WatermarkRange right)
            => !(left == right);

        public bool Equals(WatermarkRange other)
            => Start == other.Start && Count == other.Count;

        public void Deconstruct(out long start, out long end)
        {
            start = Start;
            end = End;
        }

        public override string ToString()
            => "[" + Start + ", " + (Start + Count) + ")";
    }
}
