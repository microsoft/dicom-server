// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Health.Operations;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Models.Export;

/// <summary>
/// Represents a checkpoint for the export operation which includes metadata such as the progress.
/// </summary>
public class ExportCheckpoint : ExportInput, IOperationCheckpoint
{
    /// <summary>
    /// Gets or sets the optional progress made by the operation so far.
    /// </summary>
    /// <value>The progress if any has been made so far; otherwise <see langword="null"/>.</value>
    public ExportProgress Progress { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.CreatedTime"/>
    public DateTime? CreatedTime { get; set; }

    /// <summary>
    /// Gets or sets the URI for containing the errors for this operation, if any.
    /// </summary>
    /// <value>
    /// The <see cref="Uri"/> for the resource containg export errors if it has been resolved yet;
    /// otherwise <see langword="null"/>.
    /// </value>
    public Uri ErrorHref { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.PercentComplete"/>
    [JsonIgnore]
    public int? PercentComplete => null;

    /// <inheritdoc cref="IOperationCheckpoint.ResourceIds"/>
    [JsonIgnore]
    public IReadOnlyCollection<string> ResourceIds => null;

    /// <inheritdoc cref="IOperationCheckpoint.AdditionalProperties"/>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string> AdditionalProperties =>
        new Dictionary<string, string>
        {
            { nameof(ExportProgress.Exported), Progress.Exported.ToString(CultureInfo.InvariantCulture) },
            { nameof(ExportProgress.Failed), Progress.Failed.ToString(CultureInfo.InvariantCulture) },
            { nameof(ErrorHref), ErrorHref?.AbsoluteUri.ToString(CultureInfo.InvariantCulture) },
        };
}
