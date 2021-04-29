// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Reindex;
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0052 // Remove unread private members
namespace Microsoft.Health.Dicom.Jobs
{

    public class ReindexJob : IReindexService
    {
        private readonly IServiceProvider _serviceProvider;
        public ReindexJob(IServiceProvider serviceProvider)
        {
            EnsureArg.IsNotNull(serviceProvider);
            _serviceProvider = serviceProvider;
        }

        const string RunOrchestratorName = "ReindexJob";
        const string ReindexActiviyName = "ReindexActivity";

        [FunctionName(RunOrchestratorName)]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            EnsureArg.IsNotNull(context);
            ReindexJobInput input = context.GetInput<ReindexJobInput>();
            var outputs = new List<ReindexJobEntryOutput>();


            for (long watermark = input.EndWatermark; watermark >= input.StartWatermark; watermark--)
            {
                var entryInput = new ReindexJobEntryInput() { ExtendedQueryTags = input.ExtendedQueryTags, Watermark = watermark };
                var output = await context.CallActivityAsync<ReindexJobEntryOutput>(ReindexActiviyName, entryInput);
                context.SetCustomStatus(output);
                outputs.Add(output);
            }

        }

        [FunctionName(ReindexActiviyName)]

        public ReindexJobEntryOutput Reindex([ActivityTrigger] ReindexJobEntryInput entryInput, ILogger log)
        {
            EnsureArg.IsNotNull(entryInput);
            log.LogInformation($"Reindexing {entryInput}");
            if (entryInput.Watermark == 9)
            {
                throw new ArgumentException("some exception");
            }
            Thread.Sleep(1000);
            return new ReindexJobEntryOutput();
        }

        [FunctionName("GetStatus")]
        public async Task<HttpResponseMessage> HttpGetStatus(
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
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.MaxDepth = 3;
            response.Content = new StringContent(result.ToString());
            return response;
        }


        [FunctionName("Reindex")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Reindex")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            EnsureArg.IsNotNull(starter);
            EnsureArg.IsNotNull(req);
            string tag = DicomTag.Manufacturer.GetPath();
            var input = new ReindexJobInput() { ExtendedQueryTags = new string[] { tag }, StartWatermark = 0, EndWatermark = 10 };
            string instanceId = await starter.StartNewAsync("ReindexJob", input);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        public Task<string> ReindexAsync(IEnumerable<string> extendedQueryTags, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Core.Features.Reindex.ReindexJob> GetReindexJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task CancelReindexJobAsync(string jobId, CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Task<ReindexJobReport> GetReindexJobReportAsync(string jobId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore CA1822 // Mark members as static
