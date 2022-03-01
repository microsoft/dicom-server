// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal abstract class AggregatedOrchestrationProxy<T> : OrchestrationProxy<T>
    {
        public AggregatedOrchestrationProxy(EntityId managerEntityId, IOptions<OrchestrationConcurrencyOptions> options)
            : base(managerEntityId, options)
        { }

        protected override Task<string> StartNewAsync(IDurableOrchestrationContext context, StartOrchestrationArgs<T> args)
        {
            IOrchestrationAggregate<T> aggregate = context.CreateEntityProxy<IOrchestrationAggregate<T>>(ManagerEntityId);
            return aggregate.StartAsync(args);
        }
    }
}
