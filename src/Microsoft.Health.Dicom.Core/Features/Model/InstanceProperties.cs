// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class InstanceProperties
{
    /// <summary>
    /// Transfer syntax uid of instance
    /// </summary>
    public string TransferSyntaxUid { get; init; }

    /// <summary>
    /// True if the instance has frame metadata
    /// </summary>
    public bool HasFrameMetadata { get; init; }

    /// <summary>
    /// This corresponds to the original version of the instance if the DICOM instance was updated.
    /// Used to fetch the dicom file using this version if the original version is requested.
    /// </summary>
    /// This is referenced in <see cref="InstanceFileState.OriginalVersion"/>
    public long? OriginalVersion { get; init; }

    /// <summary>
    /// This corresponds to the future current version of the instance while the DICOM instance is being updated.
    /// This is only used for the duration of the update operation.
    /// </summary>
    /// This is referenced in <see cref="InstanceFileState.NewVersion"/>
    public long? NewVersion { get; init; }

    /// <summary>
    /// File properties of instance
    /// </summary>
    public FileProperties fileProperties { get; init; }
}
