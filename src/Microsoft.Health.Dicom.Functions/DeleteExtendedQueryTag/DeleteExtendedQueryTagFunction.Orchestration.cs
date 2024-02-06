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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
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

        if (!checkpoint.Completed.HasValue && checkpoint.TagKey == default(int))
        {
            ExtendedQueryTagStoreJoinEntry xqt = await context.CallActivityWithRetryAsync<ExtendedQueryTagStoreJoinEntry>(nameof(GetExtendedQueryTagAsync), _options.RetryOptions, checkpoint.TagPath);

            // also does validation and will throw if tag is not found or is already being deleted
            await context.CallActivityWithRetryAsync(nameof(UpdateExtendedQueryTagStatusToDeleting), _options.RetryOptions, xqt.Key);

            checkpoint.TagKey = xqt.Key;
            checkpoint.VR = xqt.VR;
        }

        DeleteExtendedQueryTagArguments arguments = new DeleteExtendedQueryTagArguments
        {
            TagKey = checkpoint.TagKey,
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
                    TagKey = checkpoint.TagKey,
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
}
