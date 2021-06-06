// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Management
{
    public static class ClientProxyFunctions
    {
        [FunctionName(nameof(GetOrchestrationStatusAsync))]
        public static async Task<IActionResult> GetOrchestrationStatusAsync(
            [HttpTrigger("GET", Route = "Orchestrations/{instanceId}")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            string instanceId,
            ILogger log)
        {
            EnsureArg.IsNotNull(req, nameof(req));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(log, nameof(log));

            log.LogInformation("Querying orchestration instance with ID '{InstanceId}'", instanceId);

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return new BadRequestResult();
            }

            DurableOrchestrationStatus status = await client.GetStatusAsync(instanceId, showInput: false);
            return status == null ? new NotFoundResult() : new OkObjectResult(status);
        }
    }
}
