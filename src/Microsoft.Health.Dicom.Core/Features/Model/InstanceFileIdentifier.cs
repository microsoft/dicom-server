// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Model;

/// <summary>
/// Represents a file identifier for an instance.
/// </summary>
public class InstanceFileState
{
    /// <summary>
    /// This corresponds to the current version of the instance
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// This corresponds to the original version of the instance if the DICOM instance was updated.
    /// </summary>
    public long? OriginalVersion { get; init; }

    /// <summary>
    /// This corresponds to the future current version of the instance while the DICOM instance is being updated.
    /// </summary>
    public long? NewVersion { get; init; }
}
