// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal abstract class ThrottledOrchestrationProxy : OrchestrationProxy<object>
    {
        public ThrottledOrchestrationProxy(EntityId managerEntityId, IOptions<OrchestrationConcurrencyOptions> options)
            : base(managerEntityId, options)
        { }

        protected override Task<string> StartNewAsync(IDurableOrchestrationContext context, StartOrchestrationArgs<object> args)
        {
            IOrchestrationThrottle throttle = context.CreateEntityProxy<IOrchestrationThrottle>(ManagerEntityId);
            return throttle.StartAsync(args);
        }
    }
}
