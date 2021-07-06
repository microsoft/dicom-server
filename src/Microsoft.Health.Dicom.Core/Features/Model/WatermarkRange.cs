// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Model
{
    /// <summary>
    /// Watermark range
    /// </summary>
    public struct WatermarkRange : IEquatable<WatermarkRange>
    {
        public WatermarkRange(long start, long end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Gets or sets inclusive start watermark.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets or sets inclusive end watermark.
        /// </summary>
        public long End { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is WatermarkRange))
            {
                return false;
            }
            return Equals((WatermarkRange)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        public static bool operator ==(WatermarkRange left, WatermarkRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WatermarkRange left, WatermarkRange right)
        {
            return !(left == right);
        }

        public bool Equals(WatermarkRange other)
        {
            return Start == other.Start && End == other.End;
        }

        public void Deconstruct(out long start, out long end)
        {
            start = Start;
            end = End;
        }
    }
}
