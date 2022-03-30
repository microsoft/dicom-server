// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public readonly struct ContinuationToken : IEquatable<ContinuationToken>
{
    public long Offset { get; }

    public ContinuationToken(long value)
        => Offset = value;

    public override bool Equals(object obj)
        => obj is ContinuationToken t && Equals(t);

    public bool Equals(ContinuationToken other)
        => Offset == other.Offset;

    public override int GetHashCode()
        => Offset.GetHashCode();

    public static bool operator ==(ContinuationToken left, ContinuationToken right)
        => left.Equals(right);

    public static bool operator !=(ContinuationToken left, ContinuationToken right)
        => !left.Equals(right);
}
