// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Delete;

public readonly struct DeleteSummary : IEquatable<DeleteSummary>
{
    public int Found { get; init; }

    public int Deleted { get; init; }

    public bool Equals(DeleteSummary other)
    {
        return Found == other.Found && Deleted == other.Deleted;
    }

    public override bool Equals(object obj)
        => obj is DeleteSummary other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Found, Deleted);

    public static bool operator ==(DeleteSummary left, DeleteSummary right)
        => Equals(left, right);

    public static bool operator !=(DeleteSummary left, DeleteSummary right)
        => !Equals(left, right);
}
