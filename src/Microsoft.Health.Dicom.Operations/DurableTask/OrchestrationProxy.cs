// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Operations.DurableTask
{
    internal abstract class OrchestrationProxy
    {
        private readonly EntityId _managerEntityId;
        private readonly OrchestrationConcurrencyOptions _options;

        public OrchestrationProxy(EntityId managerEntityId, IOptions<OrchestrationConcurrencyOptions> options)
        {
            _managerEntityId = managerEntityId;
            _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        }

        public async Task WaitAsync(IDurableOrchestrationContext context, OrchestrationRequest request, ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger = context.CreateReplaySafeLogger(logger);

            // Start the orchestration through the manager
            IOrchestrationManager manager = context.CreateEntityProxy<IOrchestrationManager>(_managerEntityId);
            string instanceId = await manager.StartAsync(request);

            // Update our status with the upstream job
            context.SetCustomStatus(instanceId);

            // Wait on the upstream job
            DurableOrchestrationStatus upstreamStatus = await context.WaitForExternalEvent<DurableOrchestrationStatus>(_options.CompletionEvent);


        }
    }
}
