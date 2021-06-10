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
    public partial class ReindexDurableFunction
    {
        /// <summary>
        /// The activity to complete reindex.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="log">The log.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(CompleteReindexingTagsAsync))]
        public Task CompleteReindexingTagsAsync([ActivityTrigger] string operationId, ILogger log)
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
        [FunctionName(nameof(AddTagsAsync))]
        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddTagsAsync([ActivityTrigger] IReadOnlyList<AddExtendedQueryTagEntry> input, ILogger log)
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
        [FunctionName(nameof(PrepareReindexingTagsAsync))]
        public async Task<ReindexOperation> PrepareReindexingTagsAsync([ActivityTrigger] PrepareReindexingTagsInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Start reindex with {input}", input);
            return await _reindexStore.PrepareReindexingAsync(input.TagKeys, input.OperationId);
        }

        /// <summary>
        /// The activity to get processing query tags.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="log">The log.</param>
        /// <returns>Extended query tag store entries.</returns>
        [FunctionName(nameof(GetProcessingTagsAsync))]
        public async Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetProcessingTagsAsync([ActivityTrigger] string operationId, ILogger log)
        {
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Getting query tags which is being processed by operation {operationId}", operationId);
            var entries = await _reindexStore.GetReindexEntriesAsync(operationId);
            // only process tags which is on Processing
            var tagKeys = entries
                .Where(x => x.Status == IndexStatus.Processing)
                .Select(y => y.TagKey)
                .ToList();
            return await _extendedQueryTagStore.GetExtendedQueryTagsAsync(tagKeys);
        }

        /// <summary>
        ///  The activity to update end watermark of an operation.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="log">The log</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(UpdateReindexProgressAsync))]
        public Task UpdateReindexProgressAsync([ActivityTrigger] UpdateReindexProgressInput input, ILogger log)
        {
            EnsureArg.IsNotNull(input, nameof(input));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Updating reindex progress with {input}", input);
            return _reindexStore.UpdateReindexProgressAsync(input.OperationId, input.EndWatermark);
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

            var instanceIdentifiers = await _instanceStore.GetInstanceIdentifiersAsync(input.WatermarkRange);

            var tasks = new List<Task>();
            foreach (var instanceIdentifier in instanceIdentifiers)
            {
                tasks.Add(_instanceReindexer.ReindexInstanceAsync(input.TagStoreEntries, instanceIdentifier.Version));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        ///  Fetch schema version.
        /// </summary>
        /// <param name="context">The durable activity context.</param>
        /// <param name="log">The log</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(FetchSchemaVersionAsync))]
        public async Task FetchSchemaVersionAsync([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            // TODO: performance improvement, don't need to call service for every call.
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(log, nameof(log));
            log.LogInformation("Fetching schema version");
            int version = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default);
            _schemaInformation.Current = version;
            log.LogInformation("Schema version is {version}", version);
        }
    }
}
