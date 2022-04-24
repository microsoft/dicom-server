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
using Microsoft.Health.Dicom.Core.Models;
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

        // Are we done?
        if (input.Source == null)
        {
            logger.LogInformation("Completed export to '{Sink}'.", input.Destination.Type);
            return;
        }

        // Get batches
        logger.LogInformation(
            "Starting to export to '{Sink}' starting from DCM file #{Offset}.",
            input.Destination.Type,
            input.Progress.Exported + input.Progress.Failed + 1);
        await using IExportSource source = _sourceFactory.CreateSource(input.Source);

        // Start export in parallel
        var exportTasks = new List<Task<ExportProgress>>();
        for (int i = 0; i < input.Batching.MaxParallelCount; i++)
        {
            TypedConfiguration<ExportSourceType> batch = source.DequeueBatch(input.Batching.Size);
            if (batch == null)
                break; // All done

            exportTasks.Add(context.CallActivityWithRetryAsync<ExportProgress>(
                nameof(ExportBatchAsync),
                _options.RetryOptions,
                new ExportBatchArguments
                {
                    Destination = input.Destination,
                    Source = batch,
                }));
        }

        // Await the export and count how many instances were exported
        ExportProgress[] exportResults = await Task.WhenAll(exportTasks);
        ExportProgress result = exportResults.Aggregate<ExportProgress, ExportProgress>(default, (x, y) => x.Add(y));

        // Export the next set of batches
        context.ContinueAsNew(
            new ExportCheckpoint
            {
                Batching = input.Batching,
                CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                Destination = input.Destination,
                ErrorHref = input.ErrorHref ?? _sinkFactory.CreateSink(input.Destination, context.GetOperationId()).ErrorHref,
                Progress = input.Progress.Add(result),
                Source = source.Configuration,
            });
    }
}
