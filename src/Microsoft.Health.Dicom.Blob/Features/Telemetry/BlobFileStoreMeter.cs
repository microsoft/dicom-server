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
            _meter.CreateCounter<int>(nameof(BlobFileStoreOperationCount), description: "A blob file store operation was hit.");
    }

    /// <summary>
    /// Represents a call to the blob file store operation
    /// </summary>
    public Counter<int> BlobFileStoreOperationCount { get; }

    /// <summary>
    /// Sets telemetry dimensions based on whether or not stream lenght is supplied. Represent a hit of operation and sometimes
    /// we have stream length to report on operation. When stream not present, we do not emit that dimension.
    /// </summary>
    /// <param name="operationName">Name of operation being hit</param>
    /// <param name="streamLength">Length of stream being processed, if any</param>
    /// <returns></returns>
    public static KeyValuePair<string, object>[] BlobFileStoreOperationCountTelemetryDimension(string operationName, long? streamLength = null)
    {
        return streamLength == null
            ? new[]
            {
                new KeyValuePair<string, object>("Operation", operationName),
            }
            : new[]
            {
                new KeyValuePair<string, object>("Operation", operationName),
                new KeyValuePair<string, object>("StreamLength", streamLength),
            };
    }

    public void Dispose()
        => _meter.Dispose();
}