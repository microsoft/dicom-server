// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.Dicom.Core.Features.Telemetry;

public sealed class RetrieveMeter : IDisposable
{
    private readonly Meter _meter;

    public RetrieveMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.Retrieve", "1.0");
        RetrieveInstanceMetadataCount = _meter.CreateCounter<long>(nameof(RetrieveInstanceMetadataCount), description: "Count of metadata retrieved");
        RetrieveInstanceCount = _meter.CreateCounter<long>(nameof(RetrieveInstanceCount), description: "Count of instances retrieved");
    }

    public Counter<long> RetrieveInstanceMetadataCount { get; }

    public Counter<long> RetrieveInstanceCount { get; }

    public static KeyValuePair<string, object>[] RetrieveInstanceCountTelemetryDimension(bool isTranscoding = false, bool hasFrameMetadata = false, bool isRendered = false) =>
        new[]
        {
            new KeyValuePair<string, object>("IsTranscoding", isTranscoding),
            new KeyValuePair<string, object>("IsRendered", isRendered),
            new KeyValuePair<string, object>("HasFrameMetadata", hasFrameMetadata),
        };

    public void Dispose()
        => _meter.Dispose();
}
