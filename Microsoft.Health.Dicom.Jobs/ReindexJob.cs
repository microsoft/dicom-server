// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0052 // Remove unread private members
namespace Microsoft.Health.Dicom.Jobs
{

    public class ReindexJob
    {
        private readonly IServiceProvider _serviceProvider;
        public ReindexJob(IServiceProvider serviceProvider)
        {
            EnsureArg.IsNotNull(serviceProvider);
            _serviceProvider = serviceProvider;
        }

        [FunctionName("ReindexOrchestrator")]
        public async Task<IReadOnlyList<ReindexJobEntryOutput>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            EnsureArg.IsNotNull(context);
            ReindexJobInput input = context.GetInput<ReindexJobInput>();
            var outputs = new List<ReindexJobEntryOutput>();
            foreach (var item in input.Watermarks)
            {
                outputs.Add(await context.CallActivityAsync<ReindexJobEntryOutput>("ReindexInstanceActivity", item));
            }

            return outputs;
        }

        [FunctionName("ReindexInstanceActivity")]

        public ReindexJobEntryOutput ReindexInstance([ActivityTrigger] ReindexJobEntryInput entryInput, ILogger log)
        {
            EnsureArg.IsNotNull(entryInput);
            log.LogInformation($"Reindexing {entryInput}");
            return new ReindexJobEntryOutput();
        }

        [FunctionName("GetJobStatusAsync")]
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


        [FunctionName("CreateReindexJobAsync")]
        public async Task<HttpResponseMessage> CreateReindexJobAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Reindex")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            EnsureArg.IsNotNull(starter);
            EnsureArg.IsNotNull(req);

            // check if valid  to reindex
            string tag = DicomTag.Manufacturer.GetPath();
            var input = new ReindexJobInput() { ExtendedQueryTags = new string[] { tag }, Watermarks = new long[] { 100, 101, 102 } };
            string instanceId = await starter.StartNewAsync("ReindexOrchestrator", input);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore CA1822 // Mark members as static
