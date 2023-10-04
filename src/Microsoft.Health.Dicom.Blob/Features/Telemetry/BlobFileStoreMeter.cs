// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
    /// <param name="operationType">Represents whether operation is input (write) or output(read) </param>
    /// <param name="isExternal">Whether or not this metric is being emitted for an external store</param>
    /// <returns></returns>
    public static KeyValuePair<string, object>[] CreateBlobFileStoreOperationTelemetryDimension(string operationName, OperationType operationType, bool isExternal) =>
        new[]
        {
            new KeyValuePair<string, object>("Operation", operationName),
            new KeyValuePair<string, object>("Type", operationType),
            new KeyValuePair<string, object>("IsExternal", isExternal),
        };

    public void Dispose()
        => _meter.Dispose();
}