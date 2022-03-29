// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;
using Microsoft.Health.Dicom.Functions.Linq;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Export;

public partial class ExportDurableFunction
{
    [FunctionName(nameof(ExportDicomFilesAsync))]
    public async Task ExportDicomFilesAsync([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();

        ExportCheckpoint input = context.GetInput<ExportCheckpoint>();

        logger = context.CreateReplaySafeLogger(logger);
        logger.LogInformation("Starting to export to '{Sink}' starting from DCM file offset {Offset}.", input.Destination.Type, input.Exported);

        IEnumerable<Task<int>> exportTasks = Enumerate
            .Range(input.Exported, input.Batching.MaxParallel, input.Batching.Size)
            .Select(offset => context.CallActivityWithRetryAsync<int>(
                nameof(ExportBatchAsync),
                _options.RetryOptions,
                new ExportBatchArguments
                {
                    Batching = input.Batching,
                    Destination = input.Destination,
                    Offset = offset,
                    Source = input.Source,
                }));

        int[] exportResults = await Task.WhenAll(exportTasks);
        long exported = exportResults.Aggregate(0L, (current, next) => current + next);

        if (exportResults[^1] > 0)
        {
            context.ContinueAsNew(
                new ExportCheckpoint
                {
                    Batching = input.Batching,
                    Destination = input.Destination,
                    Exported = input.Exported + exported,
                    Source = input.Source,
                });
        }
    }
}
