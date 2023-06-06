// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models;

public readonly struct TimeRange : IEquatable<TimeRange>
{
    public static readonly TimeRange MaxValue = new TimeRange(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

    public TimeRange(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentOutOfRangeException(nameof(startTime));

        Start = startTime;
        End = endTime;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public override bool Equals(object obj)
        => obj is TimeRange other && Equals(other);

    public bool Equals(TimeRange other)
        => Start == other.Start && End == other.End;

    public override int GetHashCode()
        => HashCode.Combine(Start, End);

    public override string ToString()
        => $"[{Start:O}, {End:O})";

    public static bool operator ==(TimeRange left, TimeRange right)
        => left.Equals(right);

    public static bool operator !=(TimeRange left, TimeRange right)
        => !left.Equals(right);

    public static TimeRange After(DateTimeOffset start)
        => new TimeRange(start, DateTimeOffset.MaxValue);

    public static TimeRange Before(DateTimeOffset end)
        => new TimeRange(DateTimeOffset.MinValue, end);
}
