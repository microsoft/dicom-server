// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
#pragma warning disable CA1822 // Mark members as static
namespace Microsoft.Health.Dicom.Jobs
{

    public class ReindexJob
    {
        const string RunOrchestratorName = "ReindexJob";
        const string HttpStartName = "ReindexJob_HttpStart";
        const string ReindexActiviyName = "ReindexActivity";
        [FunctionName(RunOrchestratorName)]
        public async Task<List<ReindexJobEntryOutput>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            EnsureArg.IsNotNull(context);
            ReindexJobInput input = context.GetInput<ReindexJobInput>();
            var outputs = new List<ReindexJobEntryOutput>();

            for (long watermark = input.EndWatermark; watermark >= input.StartWatermark; watermark--)
            {
                var entryInput = new ReindexJobEntryInput() { ExtendedQueryTags = input.ExtendedQueryTags, Watermark = watermark };
                outputs.Add(await context.CallActivityAsync<ReindexJobEntryOutput>(ReindexActiviyName, entryInput));
            }

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName(ReindexActiviyName)]

        public ReindexJobEntryOutput Reindex([ActivityTrigger] ReindexJobEntryInput entryInput, ILogger log)
        {
            log.LogInformation($"Reindexing {entryInput}");
            return new ReindexJobEntryOutput();
        }

        [FunctionName(HttpStartName)]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            EnsureArg.IsNotNull(starter);
            string tag = DicomTag.Manufacturer.GetPath();
            var input = new ReindexJobInput() { ExtendedQueryTags = new string[] { tag }, StartWatermark = 0, EndWatermark = 100 };
            string instanceId = await starter.StartNewAsync("ReindexJob", input);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
