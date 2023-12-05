// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    /// <summary>
    /// Asynchronously exports DICOM files to a user-specified sink.
    /// </summary>
    /// <param name="context">The context for the orchestration instance.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="ExportDicomFilesAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">Orchestration instance ID is invalid.</exception>
    [FunctionName(nameof(ExportDicomFilesAsync))]
    public async Task ExportDicomFilesAsync([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));

        ExportCheckpoint input = context.GetInput<ExportCheckpoint>();

        // Are we done?
        if (input.Source == null)
        {
            await context.CallActivityWithRetryAsync(nameof(CompleteCopyAsync), _options.RetryOptions, input.Destination);

            logger.LogInformation("Completed export to '{Sink}'.", input.Destination.Type);
            return;
        }

        // Get batches
        logger.LogInformation(
            "Starting to export to '{Sink}'. Exported {Exported} files so far. Skipped {Skipped} resources.",
            input.Destination.Type,
            input.Progress.Exported,
            input.Progress.Skipped);

        await using IExportSource source = await _sourceFactory.CreateAsync(input.Source, input.Partition);

        // Start export in parallel
        var exportTasks = new List<Task<ExportProgress>>();
        for (int i = 0; i < input.Batching.MaxParallelCount; i++)
        {
            if (!source.TryDequeueBatch(input.Batching.Size, out ExportDataOptions<ExportSourceType> batch))
                break; // All done

            exportTasks.Add(context.CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportBatchAsync),
                _options.RetryOptions,
                new ExportBatchArguments
                {
                    Destination = input.Destination,
                    Partition = input.Partition,
                    Source = batch,
                }));
        }

        // Await the export and count how many instances were exported
        ExportProgress[] exportResults = await Task.WhenAll(exportTasks);
        ExportProgress iterationProgress = exportResults.Aggregate<ExportProgress, ExportProgress>(default, (x, y) => x + y);

        // Export the next set of batches
        context.ContinueAsNew(
            new ExportCheckpoint
            {
                Batching = input.Batching,
                CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                Destination = input.Destination,
                ErrorHref = input.ErrorHref,
                Progress = input.Progress + iterationProgress,
                Source = source.Description,
                Partition = input.Partition
            });
    }
}
