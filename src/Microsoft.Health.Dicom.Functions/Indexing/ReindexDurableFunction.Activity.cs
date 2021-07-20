// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// Asynchronously retrieves the query tags that have been associated with the operation.
        /// </summary>
        /// <remarks>
        /// If the tags were not previously associated with the operation, this operation will create the association.
        /// </remarks>
        /// <param name="context">The context for the activity.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <returns>
        /// A task representing the <see cref="GetQueryTagsAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the collection of query tags
        /// that have been associated the operation.
        /// </returns>
        [FunctionName(nameof(GetQueryTagsAsync))]
        public Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetQueryTagsAsync(
            [ActivityTrigger] IDurableActivityContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(logger, nameof(logger));

            IReadOnlyList<int> tagKeys = context.GetInput<IReadOnlyList<int>>();
            logger.LogInformation("Fetching {Count} query tags for operation ID '{OperationId}': {{{TagKeys}}}",
                tagKeys.Count,
                context.InstanceId,
                string.Join(", ", tagKeys));

            return _extendedQueryTagStore.ConfirmReindexingAsync(tagKeys, context.InstanceId, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously retrieves the maximum instance watermark.
        /// </summary>
        /// <remarks>
        /// Watermark values start at <c>1</c>.
        /// </remarks>
        /// <param name="context">The context for the activity.</param>
        /// <param name="logger">A diagnostic logger.</param>
        /// <returns>
        /// A task representing the <see cref="GetMaxInstanceWatermarkAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the maximum watermark value if found;
        /// otherwise, <c>0</c>.
        /// </returns>
        [FunctionName(nameof(GetMaxInstanceWatermarkAsync))]
        public Task<long> GetMaxInstanceWatermarkAsync(
            [ActivityTrigger] IDurableActivityContext context,
            ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Fetching the maximum instance watermark");
            return _instanceStore.GetMaxInstanceWatermarkAsync(CancellationToken.None);
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

            var tasks = new List<Task>();
            foreach (VersionedInstanceIdentifier identifier in instanceIdentifiers)
            {
                // TODO: Should this be split into two operations:
                // (1) Read all tags in blob
                // (2) Write all new indices to SQL together
                tasks.Add(_instanceReindexer.ReindexInstanceAsync(batch.QueryTags, identifier.Version));
            }

            await Task.WhenAll(tasks);
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
