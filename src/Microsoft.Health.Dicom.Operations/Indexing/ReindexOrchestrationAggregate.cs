// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Microsoft.Health.Dicom.Operations.DurableTask;

namespace Microsoft.Health.Dicom.Operations.Indexing
{
    internal class ReindexOrchestrationAggregate : OrchestrationAggregate<ReindexInput, HashSet<int>, ReindexInput>
    {
        public static EntityId Singleton { get; } = new EntityId(nameof(ReindexOrchestrationAggregate), nameof(Singleton));

        public ReindexOrchestrationAggregate(
            IDurableClient durableClient,
            IGuidFactory guidFactory,
            Func<DateTime> getUtcNow,
            IOptions<OrchestrationConcurrencyOptions> options,
            ILogger logger)
            : base(durableClient, guidFactory, getUtcNow, options, logger)
        { }

        protected override HashSet<int> Add(HashSet<int> state, ReindexInput input)
        {
            foreach (int key in input.QueryTagKeys)
            {
                state.Add(key);
            }

            return state;
        }

        protected override ReindexInput Finalize(HashSet<int> state)
        {
            return new ReindexInput { QueryTagKeys = state };
        }

        protected override HashSet<int> Initialize()
            => new HashSet<int>();

        [FunctionName(nameof(ReindexOrchestrationAggregate))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx, [DurableClient] IDurableClient client)
            => ctx.DispatchAsync<ReindexOrchestrationAggregate>(client);
    }
}
