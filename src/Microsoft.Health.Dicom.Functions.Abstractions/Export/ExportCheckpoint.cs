// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.DurableTask;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents a checkpoint for the export operation which includes metadata such as the progress.
/// </summary>
public class ExportCheckpoint : ExportInput, IOrchestrationCheckpoint
{
    /// <summary>
    /// Gets or sets the optional progress made by the operation so far.
    /// </summary>
    /// <value>The progress if any has been made so far; otherwise <see langword="null"/>.</value>
    public ExportProgress Progress { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.CreatedTime"/>
    public DateTime? CreatedTime { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.PercentComplete"/>
    [JsonIgnore]
    public int? PercentComplete => null;

    /// <inheritdoc cref="IOperationCheckpoint.ResourceIds"/>
    [JsonIgnore]
    public IReadOnlyCollection<string> ResourceIds => null;

    /// <summary>
    /// Gets the <see cref="ExportResults"/> for the orchestration.
    /// </summary>
    /// <param name="output">The unused orchestration output.</param>
    /// <returns>The current state of the orchestration.</returns>
    public object GetResults(JToken output)
        => new ExportResults(Progress, ErrorHref);
}
