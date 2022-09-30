// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Core.Configs;

public class RetrieveConfiguration
{
    /// <summary>
    /// Maximum dicom file supported for getting frames and transcoding
    /// </summary>
    public long MaxDicomFileSize { get; } = 1024 * 1024 * 100; //100 MB

    /// <summary>
    /// Response uses lazy streams that copy data from storage JIT into a buffer and then to the output stream
    /// This is the size of the buffer
    /// </summary>
    public int LazyResponseStreamBufferSize { get; } = 1024 * 1024 * 4; //4 MB

    /// <summary>
    /// Gets or sets the maximum number of tasks that should be concurrently scheduled to read for a single metadata request.
    /// </summary>
    /// <value>A positive number or <c>-1</c> for unbounded parallelism.</value>
    [Range(-1, int.MaxValue)]
    public int MaxDegreeOfParallelism { get; set; } = -1;

    /// <summary>
    /// Gets or sets the maximum number of Dicom Data Sets to buffer in memory before pausing.
    /// </summary>
    /// <remarks>
    /// Once Dicom Data Sets are read by the caller, new Data Sets will begin buffering again.
    /// </remarks>
    /// <value>A positive number.</value>
    [Range(1, int.MaxValue)]
    public int MaxBufferedDataSets { get; set; } = 100;
}
