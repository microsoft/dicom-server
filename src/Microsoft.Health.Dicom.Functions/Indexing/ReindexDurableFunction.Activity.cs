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
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// The activity to complete reindex.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="log">The log.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(CompleteReindexActivityAsync))]
        public Task CompleteReindexActivityAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Completing Reindex operation on {operationId}", operationId);
            return _reindexStore.CompleteReindexAsync(operationId);
        }

        /// <summary>
        ///  The activity to add extended query tags.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="log">The log.</param>
        /// <returns>The store entries.</returns>
        [FunctionName(nameof(AddExtendedQueryTagsActivityAsync))]
        public async Task<IEnumerable<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsActivityAsync([ActivityTrigger] IEnumerable<AddExtendedQueryTagEntry> input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Adding extended query tags with {input}", input);

            // TODO: change AddExtendedQueryTagAsync to return ExtendedQueryTagStoreEntry
            await _addExtendedQueryTagService.AddExtendedQueryTagAsync(input);
            return Array.Empty<ExtendedQueryTagStoreEntry>();
        }

        /// <summary>
        ///  The activity to start reindex.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="log">The log.</param>
        /// <returns>The reindex operation.</returns>
        [FunctionName(nameof(StartReindexActivityAsync))]
        public async Task<ReindexOperation> StartReindexActivityAsync([ActivityTrigger] StartReindexActivityInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Start reindex with {input}", input);
            return await _reindexStore.StartReindexAsync(input.TagKeys, input.OperationId);
        }

        /// <summary>
        /// The activity to get processing query tags.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="log">The log.</param>
        /// <returns>Extended query tag store entries.</returns>
        [FunctionName(nameof(GetProcessingQueryTagsActivityAsync))]
        public async Task<IEnumerable<ExtendedQueryTagStoreEntry>> GetProcessingQueryTagsActivityAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Getting query tags which is being processed by operation {operationId}", operationId);
            var entries = await _reindexStore.GetReindexEntriesAsync(operationId);
            // only process tags which is on Processing
            var tagKeys = entries.Where(x => x.Status == ReindexStatus.Processing).Select(y => y.TagKey);
            return await _extendedQueryTagStore.GetExtendedQueryTagsByKeyAsync(tagKeys);
        }

        /// <summary>
        ///  The activity to update end watermark of an operation.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="log">The log</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(UpdateReindexProgressActivityAsync))]
        public Task UpdateReindexProgressActivityAsync([ActivityTrigger] UpdateReindexProgressActivityInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Updating reindex progress with {input}", input);
            return _reindexStore.UpdateReindexProgressAsync(input.OperationId, input.EndWatermark);
        }

        /// <summary>
        /// The activity to reindex  Dicom instance.
        /// </summary>
        /// <param name="input">The input</param>
        /// <param name="logger">The log.</param>
        /// <returns>The task</returns>
        [FunctionName(nameof(ReindexInstanceActivityAsync))]
        public async Task ReindexInstanceActivityAsync([ActivityTrigger] ReindexInstanceActivityInput input, ILogger logger)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger.LogInformation("Reindex instance with with {input}", input);

            var instanceIdentifiers = await _instanceStore.GetInstanceIdentifierAsync(input.StartWatermark, input.EndWatermark);

            var tasks = new List<Task>();
            foreach (var instanceIdentifier in instanceIdentifiers)
            {
                tasks.Add(_instanceReindexer.ReindexInstanceAsync(input.TagStoreEntries, instanceIdentifier.Version));
            }

            await Task.WhenAll(tasks);
        }
    }
}
