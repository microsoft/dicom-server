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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.ContentLengthBackFill;

public partial class ContentLengthBackFillDurableFunction
{
    /// <summary>
    /// Asynchronously backfills the content length for instance data.
    /// </summary>
    /// <remarks>
    /// Durable functions are reliable, and their implementations will be executed repeatedly over the lifetime of
    /// a single instance.
    /// </remarks>
    /// <param name="context">The context for the orchestration instance.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="ContentLengthBackFillAsync"/> operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">Orchestration instance ID is invalid.</exception>
    [FunctionName(nameof(ContentLengthBackFillAsync))]
    public async Task ContentLengthBackFillAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));

        ContentLengthBackFillCheckPoint input = context.GetInput<ContentLengthBackFillCheckPoint>();

        IReadOnlyList<WatermarkRange> batches = await context.CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
            nameof(GetContentLengthBackFillInstanceBatches),
            _options.RetryOptions,
            new BatchCreationArguments(input.Batching.Size, input.Batching.MaxParallelCount));

        if (batches.Count > 0)
        {
            // Batches are in reverse order because we start from the highest watermark
            var batchRange = new WatermarkRange(batches[^1].Start, batches[0].End);

            logger.LogInformation("Beginning to back fill content length data range {Range}.", batchRange);
            await Task.WhenAll(batches
                .Select(x => context.CallActivityWithRetryAsync(
                    nameof(BackFillContentLengthRangeDataAsync),
                    _options.RetryOptions,
                    x)));

            // Create a new orchestration with the same instance ID to process the remaining data
            logger.LogInformation("Completed back fill for content length data in the range {Range}. Continuing with new execution...", batchRange);

            WatermarkRange completed = input.Completed.HasValue
                ? new WatermarkRange(batchRange.Start, input.Completed.Value.End)
                : batchRange;

            context.ContinueAsNew(
                new ContentLengthBackFillCheckPoint()
                {
                    Batching = input.Batching,
                    Completed = completed,
                    CreatedTime = input.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                });
        }
        else
        {
            logger.LogInformation("Completed back fill for content length data operation.");
        }
    }
}
