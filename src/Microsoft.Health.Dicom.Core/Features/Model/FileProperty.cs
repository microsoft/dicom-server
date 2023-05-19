// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Model;

/// <summary>
/// Representation of FilePropertyTable
/// </summary>
public class FileProperty
{
    public long Watermark { get; init; }

    public string FilePath { get; init; }

    public string ETag { get; init; }
}