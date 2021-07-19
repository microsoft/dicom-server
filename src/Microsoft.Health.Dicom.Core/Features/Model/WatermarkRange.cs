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
        public WatermarkRange(long start, int count)
        {
            Start = EnsureArg.IsGte(start, 0, nameof(start));
            Count = EnsureArg.IsGte(count, 0, nameof(count));
        }

        /// <summary>
        /// Gets inclusive start watermark.
        /// </summary>
        public long Start { get; }

        /// <summary>
        /// Gets or sets exclusive end watermark.
        /// </summary>
        public long End => Start + Count;

        /// <summary>
        /// Gets the maximum number of instances within this range.
        /// </summary>
        /// <remarks>
        /// Some instances may be missing in the range due to previous deletion operations.
        /// </remarks>
        public int Count { get; }

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
