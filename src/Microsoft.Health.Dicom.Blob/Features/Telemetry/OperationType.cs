// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob.Features.Telemetry;

/// <summary>
/// Represents whether operation is input (write) or output(read) from perspective of I/O on the blob store
/// </summary>
public enum OperationType
{
    /// <summary>
    /// For Operations that write data
    /// </summary>
    Input,

    /// <summary>
    /// For Operations that read data
    /// </summary>
    Output
}