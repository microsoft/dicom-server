// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Operations.Indexing;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    public sealed class ReindexDurableFunctionDecorator
    {
        private readonly ReindexDurableFunction _function;

        public ReindexDurableFunctionDecorator(ReindexDurableFunction function)
            => _function = EnsureArg.IsNotNull(function, nameof(function));

        [FunctionName(nameof(StartReindexingInstancesAsync))]
        public Task<HttpResponseMessage> StartReindexingInstancesAsync(
            [HttpTrigger(AuthorizationLevel.Function, "Post", Route = "extendedquerytags/reindex")] HttpRequest request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger,
            CancellationToken hostCancellationToken = default)
            => _function.StartReindexingInstancesAsync(request, client, logger, hostCancellationToken);
    }
}
