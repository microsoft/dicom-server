// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Store;
public sealed class StoreMeter : IDisposable
{
    private readonly Meter _meter;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal is done through Dipose method.")]
    public StoreMeter()
        : this(new Meter("Microsoft.Health.Cloud.Dicom.Core.Features.Store", "1.0"))
    {
    }

    internal StoreMeter(Meter meter)
    {
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        IndexTagValidationError = _meter.CreateCounter<double>(nameof(IndexTagValidationError));
        DroppedInvalidTag = _meter.CreateCounter<double>(nameof(DroppedInvalidTag));
        OldestRequestedDeletion = _meter.CreateCounter<double>(nameof(OldestRequestedDeletion));
        CountDeletionsMaxRetry = _meter.CreateCounter<double>(nameof(CountDeletionsMaxRetry));
        InstanceCount = _meter.CreateCounter<double>(nameof(InstanceCount));
        TotalInstanceBytes = _meter.CreateHistogram<double>(nameof(TotalInstanceBytes));
        MinInstanceBytes = _meter.CreateHistogram<double>(nameof(MinInstanceBytes));
        MaxInstanceBytes = _meter.CreateHistogram<double>(nameof(MaxInstanceBytes));
    }

    public Counter<double> IndexTagValidationError { get; }
    public Counter<double> DroppedInvalidTag { get; }
    public Counter<double> OldestRequestedDeletion { get; }
    public Counter<double> CountDeletionsMaxRetry { get; }
    public Counter<double> InstanceCount { get; }
    public Histogram<double> TotalInstanceBytes { get; }
    public Histogram<double> MinInstanceBytes { get; }
    public Histogram<double> MaxInstanceBytes { get; }

    public void Dispose()
        => _meter.Dispose();
}
