// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Operations.DurableTask;

namespace Microsoft.Health.Dicom.Operations.Indexing
{
    internal class ReindexDurableFunctionProxy : AggregatedOrchestrationProxy<ReindexInput>
    {
        public ReindexDurableFunctionProxy(IOptions<OrchestrationConcurrencyOptions> options)
            : base(ReindexOrchestrationAggregate.Singleton, options)
        { }

        [FunctionName(nameof(EnqueueReindexInstancesAsync))]
        public Task EnqueueReindexInstancesAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger logger)
        {
            Proxied<ReindexInput> input = context.GetInput<Proxied<ReindexInput>>();
            return WaitAsync(
                context,
                new StartOrchestrationArgs<ReindexInput>
                {
                    FunctionName = nameof(ReindexDurableFunction.ReindexInstancesAsync),
                    Input = input.Value,
                    InstanceId = input.UpstreamInstanceId,
                },
                logger);
        }
    }
}
