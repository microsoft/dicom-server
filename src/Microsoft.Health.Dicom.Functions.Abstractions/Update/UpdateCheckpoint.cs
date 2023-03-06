// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.DurableTask;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Functions.Update;

/// <summary>
/// Represents the state of the reindexing orchestration that is serialized in the input.
/// </summary>
public sealed class UpdateCheckpoint : UpdateInput, IOrchestrationCheckpoint
{
    /// <summary>
    /// Gets or sets the range of DICOM SOP instance watermarks that have been reindexed, if started.
    /// </summary>
    /// <value>A range of instance watermarks if started; otherwise, <see langword="null"/>.</value>
    public WatermarkRange? Completed { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.CreatedTime"/>
    public DateTime? CreatedTime { get; set; }

    public long? MaxWatermark { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.PercentComplete"/>
    [JsonIgnore]
    public int? PercentComplete
    {
        get
        {
            if (Completed.HasValue)
            {
                WatermarkRange range = Completed.GetValueOrDefault();
                return range.End == 1 ? 100 : (int)((double)(range.End - range.Start + 1) / range.End * 100);
            }

            return 0;
        }
    }

    public IReadOnlyCollection<string> ResourceIds => null;

    /// <inheritdoc cref="IOrchestrationCheckpoint.GetResults(JToken?)"/>
    public object GetResults(JToken output)
        => null; // TODO: Expose metrics to users via the operation API?
}
