// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Models.Export;

public readonly struct ExportProgress : IEquatable<ExportProgress>
{
    public long Exported { get; }

    public long Failed { get; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public long Total => Exported + Failed;

    public ExportProgress(long exported, long failed)
    {
        Exported = EnsureArg.IsGte(exported, 0, nameof(exported));
        Failed = EnsureArg.IsGte(failed, 0, nameof(failed));
    }

    public ExportProgress Add(ExportProgress other)
        => new ExportProgress(Exported + other.Exported, Failed + other.Failed);

    public override bool Equals(object obj)
        => obj is ExportProgress other && Equals(other);

    public bool Equals(ExportProgress other)
        => Exported == other.Exported && Failed == other.Failed;

    public override int GetHashCode()
        => HashCode.Combine(Exported, Failed);

    public static ExportProgress operator +(ExportProgress x, ExportProgress y)
        => x.Add(y);

    public static bool operator ==(ExportProgress left, ExportProgress right)
        => left.Equals(right);

    public static bool operator !=(ExportProgress left, ExportProgress right)
        => !left.Equals(right);
}
