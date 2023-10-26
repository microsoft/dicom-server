// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Delete;

public readonly struct DeleteSummary : IEquatable<DeleteSummary>
{
    public int ProcessedCount { get; init; }

    public DeleteMetrics? Metrics { get; init; }

    public bool Success { get; init; }

    public bool Equals(DeleteSummary other)
    {
        return ProcessedCount == other.ProcessedCount
            && Metrics == other.Metrics
            && Success == other.Success;
    }

    public override bool Equals(object obj)
        => obj is DeleteSummary other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(ProcessedCount, Metrics, Success);

    public static bool operator ==(DeleteSummary left, DeleteSummary right)
        => Equals(left, right);

    public static bool operator !=(DeleteSummary left, DeleteSummary right)
        => !Equals(left, right);
}
