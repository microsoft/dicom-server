// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// The activity to complete reindex.
        /// </summary>
        /// <param name="tagKeys">the tag keys.</param>
        /// <param name="log">The log.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(CompleteReindexingTagsAsync))]
        public Task CompleteReindexingTagsAsync(
            [ActivityTrigger] IReadOnlyCollection<int> tagKeys,
            ILogger log)
        {
            EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Completing Reindex operation on {tagKeys}", tagKeys);
            // TODO: update tag status to Ready
            return Task.CompletedTask;
        }

        /// <summary>
        ///  The activity to get reindex watermark range.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="log">The log.</param>
        /// <returns>The reindex operation.</returns>
        [FunctionName(nameof(GetReindexWatermarkRangeAsync))]
        public async Task<WatermarkRange> GetReindexWatermarkRangeAsync(
            [ActivityTrigger] IDurableActivityContext context,
            ILogger log)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Getting reindex watermark range");
            // TODO: get reindex watermark range
            return await Task.FromResult(new WatermarkRange(0, 0));
        }

        /// <summary>
        ///  The activity to get tag store entires.
        /// </summary>
        /// <param name="tagKeys">The tag keys.</param>
        /// <param name="log">The log.</param>
        /// <returns>The reindex operation.</returns>
        [FunctionName(nameof(GetTagStoreEntriesAsync))]
        public async Task<IReadOnlyCollection<ExtendedQueryTagStoreEntry>> GetTagStoreEntriesAsync(
            [ActivityTrigger] IReadOnlyCollection<int> tagKeys,
            ILogger log)
        {
            EnsureArg.IsNotNull(tagKeys, nameof(tagKeys));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Start getting extended query tag store entries for tag keys {input}", tagKeys);
            // TODO: get extended query tag store entries for tagkeys
            return await Task.FromResult(Array.Empty<ExtendedQueryTagStoreEntry>());
        }


        /// <summary>
        /// The activity to reindex  Dicom instances.
        /// </summary>
        /// <param name="input">The input</param>
        /// <param name="logger">The log.</param>
        /// <returns>The task</returns>
        [FunctionName(nameof(ReindexInstancesAsync))]
        public async Task ReindexInstancesAsync([ActivityTrigger] ReindexInstanceInput input, ILogger logger)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Reindex instances with {input}", input);

            var instanceIdentifiers = await _instanceStore.GetInstanceIdentifiersByWatermarkRange(input.WatermarkRange, Core.Models.IndexStatus.Created);

            var tasks = new List<Task>();
            foreach (var instanceIdentifier in instanceIdentifiers)
            {
                tasks.Add(_instanceReindexer.ReindexInstanceAsync(input.TagStoreEntries, instanceIdentifier.Version));
            }

            await Task.WhenAll(tasks);
        }
    }
}
