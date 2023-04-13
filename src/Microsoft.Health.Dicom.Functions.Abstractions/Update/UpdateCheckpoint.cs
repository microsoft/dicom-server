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

namespace Microsoft.Health.Dicom.Functions.Update;

/// <summary>
/// Represents the state of the reindexing orchestration that is serialized in the input.
/// </summary>
public sealed class UpdateCheckpoint : UpdateInput, IOrchestrationCheckpoint
{
    public int NumberOfStudyCompleted { get; set; }

    public int? TotalNumberOfStudies { get; set; }

    public int TotalNumberOfInstanceUpdated { get; set; }

    public int TotalNumberOfInstanceFailed { get; set; }

    public IReadOnlyList<string> Errors { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.CreatedTime"/>
    public DateTime? CreatedTime { get; set; }

    /// <inheritdoc cref="IOperationCheckpoint.PercentComplete"/>
    [JsonIgnore]
    public int? PercentComplete
    {
        get
        {
            return NumberOfStudyCompleted == TotalNumberOfStudies.Value ? 100 : (int)((double)(TotalNumberOfStudies.Value - NumberOfStudyCompleted + 1) / TotalNumberOfStudies.Value * 100);
        }
    }

    public IReadOnlyCollection<string> ResourceIds => null;

    public object GetResults(JToken output) => new UpdateResult(NumberOfStudyCompleted, TotalNumberOfInstanceUpdated, TotalNumberOfInstanceFailed, Errors);
}
