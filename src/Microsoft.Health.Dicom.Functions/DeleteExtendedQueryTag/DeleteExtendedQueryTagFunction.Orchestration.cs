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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public partial class DeleteExtendedQueryTagFunction
{
    [FunctionName(nameof(DeleteExtendedQueryTagAsync))]
    public async Task DeleteExtendedQueryTagAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(EnsureArg.IsNotNull(logger, nameof(logger)));

        DeleteExtendedQueryTagCheckpoint checkpoint = context.GetInput<DeleteExtendedQueryTagCheckpoint>();

        // get batches
        IReadOnlyList<WatermarkRange> batches = await context.CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
            nameof(GetExtendedQueryTagBatchesAsync),
            _options.RetryOptions,
            new BatchCreationArguments(checkpoint.TagKey, checkpoint.VR, checkpoint.Batching.Size, checkpoint.Batching.MaxParallelCount));

        if (batches.Count > 0)
        {
            // Note that batches are in reverse order because we start from the highest watermark
            var batchRange = new WatermarkRange(batches[^1].Start, batches[0].End);

            logger.LogInformation("Beginning to delete tag data in the range {Range}.", batchRange);
            await Task.WhenAll(batches
                .Select(range => context.CallActivityWithRetryAsync(
                    nameof(DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                    _options.RetryOptions,
                    new DeleteBatchArguments(checkpoint.TagKey, checkpoint.VR, range))));

            logger.LogInformation("Completed deletion of extended query tag data the range {Range}. Continuing with new execution...", batchRange);

            WatermarkRange completed = checkpoint.Completed.HasValue
                ? new WatermarkRange(batchRange.Start, checkpoint.Completed.Value.End)
                : batchRange;

            context.ContinueAsNew(
                new DeleteExtendedQueryTagCheckpoint
                {
                    Batching = checkpoint.Batching,
                    Completed = completed,
                    CreatedTime = checkpoint.CreatedTime ?? await context.GetCreatedTimeAsync(_options.RetryOptions),
                    TagKey = checkpoint.TagKey,
                    VR = checkpoint.VR,
                });
        }
        else
        {
            // once everything else is complete, delete the tag entry itself
            await context.CallActivityWithRetryAsync(
                nameof(DeleteExtendedQueryTagEntry),
                _options.RetryOptions,
                new DeleteExtendedQueryTagArguments { TagKey = checkpoint.TagKey });

            logger.LogInformation(
                "Completed deletion of the extended query tag for tag key {TagKey}.",
                checkpoint.TagKey);
        }
    }
}
