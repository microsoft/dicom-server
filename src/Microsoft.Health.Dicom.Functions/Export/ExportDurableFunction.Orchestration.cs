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
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export.Models;
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
        logger.LogInformation("Starting to export to '{Sink}' starting from DCM file offset {Offset}.", input.Destination.Type, input.ContinuationToken.Offset);

        // Get batches
        IExportSource source = _sourceFactory.CreateSource(input.Source);
        PaginatedResults<IReadOnlyCollection<long>> offsets = source.GetBatchOffsets(input.Batching.Size, input.ContinuationToken);

        // Start export in parallel
        IEnumerable<Task<ExportResult>> exportTasks = offsets
            .Result
            .Select(offset => context.CallActivityWithRetryAsync<ExportResult>(
                nameof(ExportBatchAsync),
                _options.RetryOptions,
                new ExportBatchArguments
                {
                    Batching = input.Batching,
                    Destination = input.Destination,
                    Offset = offset,
                    Source = input.Source,
                }));

        // Await the export and count how many instances were exported
        ExportResult[] exportResults = await Task.WhenAll(exportTasks);
        ExportResult result = exportResults.Aggregate(
            (Exported: 0, Failed: 0),
            (state, partial) => (state.Exported + partial.Exported, state.Failed + partial.Failed),
            state => new ExportResult { Exported = state.Exported, Failed = state.Failed, });

        // Continue exporting if we detect there is still data to export
        if (offsets.ContinuationToken.HasValue && !exportResults[^1].IsEmpty)
        {
            context.ContinueAsNew(
                new ExportCheckpoint
                {
                    Batching = input.Batching,
                    ContinuationToken = offsets.ContinuationToken.GetValueOrDefault(),
                    Destination = input.Destination,
                    Exported = input.Exported + result.Exported,
                    Failed = input.Failed + result.Failed,
                    Source = input.Source,
                });
        }
    }
}
