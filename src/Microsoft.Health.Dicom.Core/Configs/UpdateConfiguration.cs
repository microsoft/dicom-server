// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs;

public class UpdateConfiguration
{
    /// <summary>
    /// Per block max size. 4MB by default.
    /// </summary>
    public int StageBlockSizeInBytes { get; set; } = 1024 * 1024 * 4;

    /// <summary>
    /// Max size of a large DICOM item. 1MB by default
    /// </summary>
    public int LargeDicomItemsizeInBytes { get; set; } = 1024 * 1024;
}
