// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Delete;

public readonly struct DeleteMetrics : IEquatable<DeleteMetrics>
{
    public DateTimeOffset OldestDeletion { get; init; }

    public int TotalExhaustedRetries { get; init; }

    public bool Equals(DeleteMetrics other)
    {
        return OldestDeletion == other.OldestDeletion
            && TotalExhaustedRetries == other.TotalExhaustedRetries;
    }

    public override bool Equals(object obj)
        => obj is DeleteMetrics other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(OldestDeletion, TotalExhaustedRetries);

    public static bool operator ==(DeleteMetrics left, DeleteMetrics right)
        => Equals(left, right);

    public static bool operator !=(DeleteMetrics left, DeleteMetrics right)
        => !Equals(left, right);
}
