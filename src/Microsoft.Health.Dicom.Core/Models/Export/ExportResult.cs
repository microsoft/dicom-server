// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Models.Export;

[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Data contract that is not compared.")]
public readonly struct ExportResult
{
    public long Exported { get; }

    public long Failed { get; }

    public ExportResult(long exported, long failed)
    {
        Exported = EnsureArg.IsGte(exported, 0, nameof(exported));
        Failed = EnsureArg.IsGte(failed, 0, nameof(failed));
    }

    public ExportResult Add(ExportResult other)
        => new ExportResult(Exported + other.Exported, Failed + other.Failed);

    public static ExportResult operator +(ExportResult x, ExportResult y)
        => x.Add(y);
}
