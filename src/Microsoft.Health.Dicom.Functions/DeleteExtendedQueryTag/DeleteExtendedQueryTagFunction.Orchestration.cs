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

        int tagKey = await GetExtendedQueryTagAndUpdateStatusToDeleting(context, checkpoint);

        checkpoint.TagKey = tagKey;
        DeleteExtendedQueryTagArguments arguments = new DeleteExtendedQueryTagArguments
        {
            TagKey = tagKey,
            VR = checkpoint.VR,
        };


        // get batches
        IReadOnlyList<WatermarkRange> batches = await context.CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
            nameof(GetExtendedQueryTagBatchesAsync),
            _options.RetryOptions,
            new BatchCreationArguments(arguments, checkpoint.Batching.Size, checkpoint.Batching.MaxParallelCount));

        if (batches.Count > 0)
        {
            // Note that batches are in reverse order because we start from the highest watermark
            var batchRange = new WatermarkRange(batches[^1].Start, batches[0].End);

            logger.LogInformation("Beginning to delete tag data in the range {Range}.", batchRange);
            await Task.WhenAll(batches
                .Select(range => context.CallActivityWithRetryAsync(
                    nameof(DeleteExtendedQueryTagDataByWatermarkRangeAsync),
                    _options.RetryOptions,
                    new DeleteBatchArguments(arguments, range))));

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
                    TagKey = tagKey,
                });
        }
        else
        {
            // once everything else is complete, delete the tag entry itself
            await context.CallActivityWithRetryAsync(nameof(DeleteExtendedQueryTagEntry), _options.RetryOptions, arguments);

            logger.LogInformation(
                "Completed deletion of the extended query tag for path {TagPath}.",
                checkpoint.TagPath);
        }
    }

    /// <summary>
    /// Get the tag key to delete and update the status to deleting if it is not already in that state.
    /// If it is the first time this orchestration executes, we will update the status and get the key.
    /// Otherwise, we just return the previously saved key.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="checkpoint"></param>
    /// <returns></returns>
    private Task<int> GetExtendedQueryTagAndUpdateStatusToDeleting(IDurableOrchestrationContext context, DeleteExtendedQueryTagCheckpoint checkpoint)
        => checkpoint.Completed.HasValue && checkpoint.TagKey != default(int)
        ? Task.FromResult(checkpoint.TagKey)
        : context.CallActivityWithRetryAsync<int>(nameof(GetExtendedQueryTagAndUpdateStatusToDeletingAsync), _options.RetryOptions, checkpoint.TagPath);
}
