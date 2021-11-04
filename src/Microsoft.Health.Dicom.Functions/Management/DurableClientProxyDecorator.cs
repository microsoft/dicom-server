// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Operations.Management;

namespace Microsoft.Health.Dicom.Functions.Management
{
    public sealed class DurableClientProxyDecorator
    {
        private readonly DurableClientProxy _proxy;

        public DurableClientProxyDecorator(DurableClientProxy proxy)
            => _proxy = EnsureArg.IsNotNull(proxy, nameof(proxy));

        [FunctionName(nameof(GetStatusAsync))]
        public Task<HttpResponseMessage> GetStatusAsync(
            [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "Orchestrations/Instances/{instanceId}")] HttpRequest request,
            [DurableClient] IDurableOrchestrationClient client,
            Guid instanceId,
            ILogger logger,
            CancellationToken hostCancellationToken = default)
            => _proxy.GetStatusAsync(request, client, instanceId, logger, hostCancellationToken);
    }
}
