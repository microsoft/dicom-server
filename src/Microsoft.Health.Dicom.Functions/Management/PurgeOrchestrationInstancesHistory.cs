// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Functions.Management
{
    public class PurgeOrchestrationInstancesHistory
    {
        private readonly OrchestrationsHistoryConfiguration _purgeConfig;
        private readonly Func<DateTime> _getUtcNow;

        public PurgeOrchestrationInstancesHistory(IOptions<OrchestrationsHistoryConfiguration> cleanupOptions)
            : this(cleanupOptions, () => DateTime.UtcNow)
        { }

        internal PurgeOrchestrationInstancesHistory(IOptions<OrchestrationsHistoryConfiguration> cleanupOptions, Func<DateTime> getDateTimeUtcNow)
        {
            EnsureArg.IsNotNull(cleanupOptions?.Value, nameof(cleanupOptions));
            EnsureArg.IsNotNull(getDateTimeUtcNow, nameof(getDateTimeUtcNow));
            _purgeConfig = cleanupOptions?.Value;
            _getUtcNow = getDateTimeUtcNow;
        }

        [FunctionName(nameof(PurgeOrchestrationInstancesHistory))]
        public async Task Run(
            [TimerTrigger(OrchestrationsHistoryConfiguration.PurgeFrequencyVariable)] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log,
            CancellationToken hostCancellationToken)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(myTimer, nameof(myTimer));
            EnsureArg.IsNotNull(log, nameof(log));

            List<string> orchestrationInstanceIdList = new List<string>();

            log.LogInformation("Purging orchestration instance history at: {Timestamp}", _getUtcNow());
            if (myTimer.IsPastDue)
            {
                log.LogWarning("Current function invocation is later than scheduled.");
            }

            // Specify conditions for orchestration instances.
            OrchestrationStatusQueryCondition condition = new OrchestrationStatusQueryCondition
            {
                RuntimeStatus = _purgeConfig.RuntimeStatuses,
                CreatedTimeFrom = DateTime.MinValue,
                CreatedTimeTo = _getUtcNow().AddDays(-_purgeConfig.MinimumAgeDays),
                ContinuationToken = null
            };

            do
            {
                OrchestrationStatusQueryResult listOfOrchestrators =
                   await client.ListInstancesAsync(condition, hostCancellationToken);
                condition.ContinuationToken = listOfOrchestrators.ContinuationToken;

                // Loop through the orchestration instances and purge them.
                foreach (DurableOrchestrationStatus orchestration in listOfOrchestrators.DurableOrchestrationState)
                {
                    orchestrationInstanceIdList.Add(orchestration.InstanceId);
                    await client.PurgeInstanceHistoryAsync(orchestration.InstanceId);
                }
            } while (condition.ContinuationToken != null);

            if (orchestrationInstanceIdList.Count != 0)
            {
                log.LogInformation("{Count} Durable Functions cleaned up successfully.", orchestrationInstanceIdList.Count);
                log.LogDebug("List of cleaned instance IDs: {list}", String.Join(", ", orchestrationInstanceIdList));
            }
            else
            {
                log.LogInformation("No Orchestration instances found within given conditions.");
            }
        }
    }
}
