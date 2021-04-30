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
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Jobs
{
#pragma warning disable CA1822 // Mark members as static

    public class ReindexJob
    {

        private readonly IIndexDataStore _indexDataStore;

        public ReindexJob(IIndexDataStore indexDataStore)
        {
            EnsureArg.IsNotNull(indexDataStore);
            _indexDataStore = indexDataStore;
        }

        [FunctionName(nameof(RunOrchestrator))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            EnsureArg.IsNotNull(context);
            ReindexJobInput input = context.GetInput<ReindexJobInput>();
            ReindexJobEntryInput entryInput = new ReindexJobEntryInput() { ExtendedQueryTags = input.ExtendedQueryTags, MaxWatermark = input.MaxWatermark, TopN = 1 };
            while (true)
            {
                ReindexJobEntryOutput entryOutput = await context.CallActivityAsync<ReindexJobEntryOutput>(nameof(ReindexNextNInstance), entryInput);
                if (entryOutput.NextMaxWatermark < 0)
                {
                    break;
                }
                else
                {
                    entryInput = new ReindexJobEntryInput() { ExtendedQueryTags = input.ExtendedQueryTags, MaxWatermark = entryOutput.NextMaxWatermark, TopN = 1 };
                    // TODO: should delay before next Reindex
                }
            }
        }

        [FunctionName(nameof(ReindexNextNInstance))]

        public async Task<ReindexJobEntryOutput> ReindexNextNInstance([ActivityTrigger] ReindexJobEntryInput entryInput, ILogger log)
        {
            EnsureArg.IsNotNull(entryInput);
            log.LogInformation($"Reindexing {entryInput}");
            var instances = await _indexDataStore.FetchRecentNInstancesAsync(entryInput.MaxWatermark, entryInput.TopN);

            foreach (var instance in instances)
            {
                await _indexDataStore.ReindexInstanceAsync(entryInput.ExtendedQueryTags, instance.Version);
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
            string tag = DicomTag.Manufacturer.GetPath();
            var input = new ReindexJobInput() { ExtendedQueryTags = new string[] { tag }, MaxWatermark = 102 };
            string instanceId = await starter.StartNewAsync("ReindexOrchestrator", input);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
#pragma warning restore CA1822 // Mark members as static
}

