using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Blob.Features.Telemetry;

public sealed class BlobFileStoreMeter : IDisposable
{
    private readonly Meter _meter;

    public BlobFileStoreMeter()
    {
        _meter = new Meter($"{OpenTelemetryLabels.BaseMeterName}.BlobFileStore", "1.0");

        BlobFileStoreOperationCount =
            _meter.CreateCounter<int>(nameof(BlobFileStoreOperationCount),
                description: "A blob file store operation was hit.");

        BlobFileStoreOperationStreamSize =
            _meter.CreateCounter<long>(nameof(BlobFileStoreOperationStreamSize),
                description: "The stream size being processed fo I/O.");
    }

    /// <summary>
    /// Represents a call to the blob file store operation
    /// </summary>
    public Counter<int> BlobFileStoreOperationCount { get; }

    /// <summary>
    /// streaming size given an operation
    /// </summary>
    public Counter<long> BlobFileStoreOperationStreamSize { get; }

    /// <summary>
    /// Sets telemetry dimensions on meter
    /// </summary>
    /// <param name="operationName">Name of operation being hit</param>
    /// <returns></returns>
    public static KeyValuePair<string, object>[] BlobFileStoreOperationTelemetryDimension(string operationName) =>
        new[]
        {
            new KeyValuePair<string, object>("Operation", operationName)
        };

    public void Dispose()
        => _meter.Dispose();
}