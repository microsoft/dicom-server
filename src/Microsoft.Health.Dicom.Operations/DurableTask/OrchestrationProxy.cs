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
    internal abstract class OrchestrationProxy<T>
    {
        private readonly OrchestrationConcurrencyOptions _options;

        public OrchestrationProxy(EntityId managerEntityId, IOptions<OrchestrationConcurrencyOptions> options)
        {
            ManagerEntityId = managerEntityId;
            _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        }

        protected EntityId ManagerEntityId { get; }

        public async Task<DurableOrchestrationStatus> WaitAsync(IDurableOrchestrationContext context, StartOrchestrationArgs<T> args, ILogger logger)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(args, nameof(args));
            EnsureArg.IsNotNull(logger, nameof(logger));

            logger = context.CreateReplaySafeLogger(logger);

            // Start the orchestration through the manager
            string instanceId = await StartNewAsync(context, args);

            // Update our status with the upstream job
            context.SetCustomStatus(instanceId);
            logger.LogInformation("Upstream orchestration instance ID is '{InstanceId}'.", instanceId);

            // Wait on the upstream job
            DurableOrchestrationStatus upstreamStatus = await context.WaitForExternalEvent<DurableOrchestrationStatus>(_options.CompletionEvent);
            if (upstreamStatus.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                logger.LogInformation("Upstream orchestration instance '{InstanceId}' completed successfully.", instanceId);
            }
            else
            {
                logger.LogWarning("Orchestration instance '{InstanceId}' completed with non-success status '{Status}'.", instanceId, upstreamStatus.RuntimeStatus);
            }

            return upstreamStatus;
        }

        protected abstract Task<string> StartNewAsync(IDurableOrchestrationContext context, StartOrchestrationArgs<T> args);
    }
}
