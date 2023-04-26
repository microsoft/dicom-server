// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models;

public readonly struct DateTimeOffsetRange : IEquatable<DateTimeOffsetRange>
{
    public static readonly DateTimeOffsetRange MaxValue = new DateTimeOffsetRange(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

    public DateTimeOffsetRange(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentOutOfRangeException(nameof(startTime), DicomCoreResource.TimeRangeEndBeforeStart);

        Start = startTime;
        End = endTime;
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public override bool Equals(object obj)
        => obj is DateTimeOffsetRange other && Equals(other);

    public bool Equals(DateTimeOffsetRange other)
        => Start == other.Start && End == other.End;

    public override int GetHashCode()
        => HashCode.Combine(Start, End);

    public override string ToString()
        => $"[{Start:O}, {End:O})";

    public static bool operator ==(DateTimeOffsetRange left, DateTimeOffsetRange right)
        => left.Equals(right);

    public static bool operator !=(DateTimeOffsetRange left, DateTimeOffsetRange right)
        => !left.Equals(right);

    public static DateTimeOffsetRange After(DateTimeOffset start)
        => new DateTimeOffsetRange(start, DateTimeOffset.MaxValue);

    public static DateTimeOffsetRange Before(DateTimeOffset end)
        => new DateTimeOffsetRange(DateTimeOffset.MinValue, end);
}
