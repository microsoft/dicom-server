// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Reindex;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Jobs
{
    public class ReindexJob
    {

        private readonly IIndexDataStore _indexDataStore;
        private readonly IExtendedQueryTagJobStore _reindexJobStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;

        public ReindexJob(IIndexDataStore indexDataStore, IExtendedQueryTagJobStore reindexJobStore, IExtendedQueryTagStore extendedQueryTagStore)
        {
            EnsureArg.IsNotNull(indexDataStore);
            EnsureArg.IsNotNull(reindexJobStore);
            EnsureArg.IsNotNull(extendedQueryTagStore);
            _indexDataStore = indexDataStore;
            _reindexJobStore = reindexJobStore;
            _extendedQueryTagStore = extendedQueryTagStore;
        }

        [FunctionName(nameof(ReindexOrchestrator))]
        public async Task ReindexOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            EnsureArg.IsNotNull(context);
            ReindexJobInput input = context.GetInput<ReindexJobInput>();
            ReindexJobEntryInput entryInput = new ReindexJobEntryInput() { MaxWatermark = input.MaxWatermark };
            while (true)
            {
                ReindexJobEntryOutput entryOutput = await context.CallActivityAsync<ReindexJobEntryOutput>(nameof(ReindexNextInstance), entryInput);
                if (entryOutput.NextMaxWatermark < 0)
                {
                    break;
                }
                else
                {
                    entryInput = new ReindexJobEntryInput() { MaxWatermark = entryOutput.NextMaxWatermark };
                    // TODO: should delay before next Reindex
                }
            }
        }

        [FunctionName(nameof(ReindexNextInstance))]

        public async Task<ReindexJobEntryOutput> ReindexNextInstance([ActivityTrigger] ReindexJobEntryInput entryInput, ILogger log)
        {
            EnsureArg.IsNotNull(entryInput);
            log.LogInformation($"Reindexing {entryInput}");
            var instances = await _indexDataStore.FetchRecentNInstancesAsync(entryInput.MaxWatermark, 1);

            foreach (var instance in instances)
            {
                // get qualified tags
                var jobTags = await _reindexJobStore.GetExtendedQueryTagJobStoreEntryAsync(entryInput.JobId);
                jobTags = jobTags.Where(x => x.Status == ExtendedQueryTagJobStatus.Executing);
                var tags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(jobTags.Select(x => x.TagKey));

                // start reindex
                await _indexDataStore.ReindexInstanceAsync(tags.Select(x => new QueryTag(x)), instance.Version);
            }

            long min = instances.Count == 0 ? -1 : instances.Min(x => x.Version) - 1;
            return new ReindexJobEntryOutput() { NextMaxWatermark = min };
        }

        [FunctionName(nameof(GetJobStatusAsync))]

        public async Task<HttpResponseMessage> GetJobStatusAsync(

           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetStatus")] HttpRequest req,
           [DurableClient] IDurableOrchestrationClient starter,
           ILogger log)
        {
            EnsureArg.IsNotNull(starter);
            EnsureArg.IsNotNull(req);
            string instanceId = req.Query["id"];
            log.LogInformation($"Query status of instance '{instanceId}'.");
            DurableOrchestrationStatus result = await starter.GetStatusAsync(instanceId, showHistory: true, true, true);

            HttpResponseMessage response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(result.ToString());
            return response;
        }


        [FunctionName(nameof(CreateReindexJobAsync))]
        public async Task<HttpResponseMessage> CreateReindexJobAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Reindex")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            EnsureArg.IsNotNull(starter);
            EnsureArg.IsNotNull(req);

            // check if valid  to reindex
            ReindexJobInput input = GetInput();
            string instanceId = await starter.StartNewAsync(nameof(ReindexOrchestrator), input);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            // Add to ReindexTable

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private static ReindexJobInput GetInput()
        {
            string tag = DicomTag.Manufacturer.GetPath();
            var input = new ReindexJobInput();
            return input;
        }
    }

}

