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
        private readonly PurgeOrchestrationInstancesHistoryConfiguration _cleanupConfig;
        private readonly Func<DateTime> _getUtcNow;

        public PurgeOrchestrationInstancesHistory(IOptions<PurgeOrchestrationInstancesHistoryConfiguration> cleanupOptions)
            : this(cleanupOptions, () => DateTime.UtcNow)
        { }

        internal PurgeOrchestrationInstancesHistory(IOptions<PurgeOrchestrationInstancesHistoryConfiguration> cleanupOptions, Func<DateTime> getDateTimeUtcNow)
        {
            EnsureArg.IsNotNull(cleanupOptions, nameof(cleanupOptions));
            EnsureArg.IsNotNull(getDateTimeUtcNow, nameof(getDateTimeUtcNow));
            _cleanupConfig = cleanupOptions?.Value;
            _getUtcNow = getDateTimeUtcNow;
        }

        [FunctionName(nameof(PurgeOrchestrationInstancesHistory))]
        public async Task Run([TimerTrigger("*/15 * * * * *")] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log,
            CancellationToken hostCancellationToken)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(myTimer, nameof(myTimer));

            List<string> orchestrationInstanceIdList = new List<string>();
            log.LogInformation("C# Timer trigger function executed at: {Timestamp}", _getUtcNow());

            if (myTimer.IsPastDue) log.LogWarning("Current function invocation is later than scheduled.");

            // Specify conditions for orchestration instances.
            OrchestrationStatusQueryCondition condition = new OrchestrationStatusQueryCondition
            {
                RuntimeStatus = _cleanupConfig.RuntimeStatuses,
                CreatedTimeFrom = DateTime.MinValue,
                CreatedTimeTo = _getUtcNow().AddDays(-_cleanupConfig.MinimumAgeDays),
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

            log.LogInformation("{Count} Durable Functions cleaned up successfully. List of cleaned instance IDs:\n{list}", orchestrationInstanceIdList.Count, String.Join(",\n", orchestrationInstanceIdList));
        }
    }
}
