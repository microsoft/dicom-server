// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Dicom.Functions.Indexing;

public partial class ReindexDurableFunction
{
    /// <summary>
    /// Asynchronously assigns the <see cref="IDurableActivityContext.InstanceId"/> to the given tag keys.
    /// </summary>
    /// <remarks>
    /// If the tags were not previously associated with the operation ID, this operation will create the association.
    /// </remarks>
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="AssignReindexingOperationAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the subset of query tags
    /// that have been associated the operation.
    /// </returns>
    [FunctionName(nameof(AssignReindexingOperationAsync))]
    public Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AssignReindexingOperationAsync(
        [ActivityTrigger] IDurableActivityContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<int> tagKeys = context.GetInput<IReadOnlyList<int>>();
        logger.LogInformation("Assigning {Count} query tags to operation ID '{OperationId}': {{{TagKeys}}}",
            tagKeys.Count,
            context.InstanceId,
            string.Join(", ", tagKeys));

        return _extendedQueryTagStore.AssignReindexingOperationAsync(
            tagKeys,
            context.GetOperationId(),
            returnIfCompleted: false,
            cancellationToken: CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously retrieves the query tags that have been associated with the operation.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="GetQueryTagsAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the subset of query tags
    /// that have been associated the operation.
    /// </returns>
    [FunctionName(nameof(GetQueryTagsAsync))]
    public Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetQueryTagsAsync(
        [ActivityTrigger] IDurableActivityContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation(
            "Fetching the extended query tags for operation ID '{OperationId}'.",
            context.InstanceId);

        return _extendedQueryTagStore.GetExtendedQueryTagsAsync(
            context.GetOperationId(),
            cancellationToken: CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options.
    /// </summary>
    /// <param name="maxWatermark">The optional inclusive maximum watermark.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// </returns>
    [Obsolete("Please use GetInstanceBatchesV2Async instead.")]
    [FunctionName(nameof(GetInstanceBatchesAsync))]
    public Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesAsync([ActivityTrigger] long? maxWatermark, ILogger logger)
        => GetInstanceBatchesV2Async(new BatchCreationArguments(maxWatermark, _options.BatchSize, _options.MaxParallelBatches), logger);

    /// <summary>
    /// Asynchronously retrieves the next set of instance batches based on the configured options.
    /// </summary>
    /// <param name="arguments">The options for configuring the batches.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of batches as defined by their smallest and largest watermark.
    /// </returns>
    [FunctionName(nameof(GetInstanceBatchesV2Async))]
    public Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesV2Async(
        [ActivityTrigger] BatchCreationArguments arguments,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        if (arguments.MaxWatermark.HasValue)
        {
            logger.LogInformation("Dividing up the instances into batches starting from the largest watermark {Watermark}.", arguments.MaxWatermark);
        }
        else
        {
            logger.LogInformation("Dividing up the instances into batches starting from the end.");
        }

        return _instanceStore.GetInstanceBatchesAsync(
            arguments.BatchSize,
            arguments.MaxParallelBatches,
            IndexStatus.Created,
            arguments.MaxWatermark,
            CancellationToken.None);
    }

    /// <summary>
    /// Asynchronously re-indexes a range of data.
    /// </summary>
    /// <param name="batch">The batch that should be re-indexed including the range of data and the new tags.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="ReindexBatchAsync"/> operation.</returns>
    [Obsolete("Please use ReindexBatchV2Async instead.")]
    [FunctionName(nameof(ReindexBatchAsync))]
    public Task ReindexBatchAsync([ActivityTrigger] ReindexBatch batch, ILogger logger)
        => ReindexBatchV2Async(batch?.ToArguments(_options.BatchThreadCount), logger);

    /// <summary>
    /// Asynchronously re-indexes a range of data.
    /// </summary>
    /// <param name="arguments">The options that include the instances to re-index and the query tags.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the <see cref="ReindexBatchAsync"/> operation.</returns>
    [FunctionName(nameof(ReindexBatchV2Async))]
    public async Task ReindexBatchV2Async([ActivityTrigger] ReindexBatchArguments arguments, ILogger logger)
    {
        EnsureArg.IsNotNull(arguments, nameof(arguments));
        EnsureArg.IsNotNull(logger, nameof(logger));

        string tags = string.Join(", ", arguments.QueryTags.Select(x => x.Path));
        logger.LogInformation("Beginning to re-index instances in the range {Range} for the {TagCount} query tags {{{Tags}}}",
            arguments.WatermarkRange,
            arguments.QueryTags.Count,
            tags);

        IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
            await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(arguments.WatermarkRange, IndexStatus.Created);

        for (int i = 0; i < instanceIdentifiers.Count; i += arguments.ThreadCount)
        {
            var tasks = new List<Task>();
            for (int j = i; j < Math.Min(instanceIdentifiers.Count, i + arguments.ThreadCount); j++)
            {
                tasks.Add(_instanceReindexer.ReindexInstanceAsync(arguments.QueryTags, instanceIdentifiers[j]));
            }

            await Task.WhenAll(tasks);
        }

        logger.LogInformation("Completed re-indexing instances in the range {Range} for the {TagCount} query tags {{{Tags}}}",
            arguments.WatermarkRange,
            arguments.QueryTags.Count,
            tags);
    }

    /// <summary>
    /// Asynchronously completes the operation by removing the association between the tags and the operation.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="CompleteReindexingAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the set of extended query tags
    /// whose re-indexing should be considered completed.
    /// </returns>
    [FunctionName(nameof(CompleteReindexingAsync))]
    public Task<IReadOnlyList<int>> CompleteReindexingAsync(
        [ActivityTrigger] IDurableActivityContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IReadOnlyList<int> tagKeys = context.GetInput<IReadOnlyList<int>>();
        logger.LogInformation("Completing the re-indexing operation {OperationId} for {Count} query tags {{{TagKeys}}}",
            context.InstanceId,
            tagKeys.Count,
            string.Join(", ", tagKeys));

        return _extendedQueryTagStore.CompleteReindexingAsync(tagKeys, CancellationToken.None);
    }
}
