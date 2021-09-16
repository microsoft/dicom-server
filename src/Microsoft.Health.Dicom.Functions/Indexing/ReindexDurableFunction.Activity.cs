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
using Microsoft.Health.Dicom.Functions.Extensions;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
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
                context.GetInstanceGuid(),
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
                context.GetInstanceGuid(),
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
        [FunctionName(nameof(GetInstanceBatchesAsync))]
        public Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesAsync(
            [ActivityTrigger] long? maxWatermark,
            ILogger logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));

            if (maxWatermark.HasValue)
            {
                logger.LogInformation("Dividing up the instances into batches starting from {Watermark}.", maxWatermark);
            }
            else
            {
                logger.LogInformation("Dividing up the instances into batches starting from the end.");
            }

            return _instanceStore.GetInstanceBatchesAsync(
                _options.BatchSize,
                _options.MaxParallelBatches,
                IndexStatus.Created,
                maxWatermark,
                CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously re-indexes a range of data.
        /// </summary>
        /// <param name="batch">The batch that should be re-indexed including the range of data and the new tags.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <returns>A task representing the <see cref="ReindexBatchAsync"/> operation.</returns>
        [FunctionName(nameof(ReindexBatchAsync))]
        public async Task ReindexBatchAsync([ActivityTrigger] ReindexBatch batch, ILogger logger)
        {
            EnsureArg.IsNotNull(batch, nameof(batch));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Re-indexing instances in the range {Range} for the {TagCount} query tags {{{Tags}}}",
                batch.WatermarkRange,
                batch.QueryTags.Count,
                string.Join(", ", batch.QueryTags.Select(x => x.Path)));

            IReadOnlyList<VersionedInstanceIdentifier> instanceIdentifiers =
                await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(batch.WatermarkRange, IndexStatus.Created);

            for (int i = 0; i < instanceIdentifiers.Count; i += _options.BatchThreadCount)
            {
                var tasks = new List<Task>();
                for (int j = i; j < Math.Min(instanceIdentifiers.Count, i + _options.BatchThreadCount); j++)
                {
                    tasks.Add(_instanceReindexer.ReindexInstanceAsync(batch.QueryTags, instanceIdentifiers[j]));
                }

                await Task.WhenAll(tasks);
            }
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
}
