// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Migration.Models;

/// <summary>
///  Represents input to <see cref="MigrationFilesDurableFunction.MigrateFrameRangeFilesAsync"/>
/// </summary>
public sealed class MigrationFilesBatchArguments
{
    /// <summary>
    /// Gets or sets the inclusive watermark range.
    /// </summary>
    public WatermarkRange WatermarkRange { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationFilesBatchArguments"/> class with the specified values.
    /// </summary>
    /// <param name="watermarkRange">The inclusive watermark range.</param>
    public MigrationFilesBatchArguments(WatermarkRange watermarkRange)
    {
        WatermarkRange = watermarkRange;
    }
}
