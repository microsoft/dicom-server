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
using Microsoft.Health.Dicom.Functions.Configuration;

namespace Microsoft.Health.Dicom.Functions.Management
{
    public class PurgeOrchestrationInstanceHistory
    {
        private readonly PurgeHistoryOptions _purgeConfig;
        private readonly Func<DateTime> _getUtcNow;

        public const string PurgeFrequencyVariable = "%"
            + DicomFunctionsConfiguration.HostSectionName + ":"
            + DicomFunctionsConfiguration.SectionName + ":"
            + PurgeHistoryOptions.SectionName + ":"
            + nameof(PurgeHistoryOptions.Frequency) + "%";

        public PurgeOrchestrationInstanceHistory(IOptions<PurgeHistoryOptions> cleanupOptions)
            : this(cleanupOptions, () => DateTime.UtcNow)
        { }

        internal PurgeOrchestrationInstanceHistory(IOptions<PurgeHistoryOptions> cleanupOptions, Func<DateTime> getDateTimeUtcNow)
        {
            _purgeConfig = EnsureArg.IsNotNull(cleanupOptions?.Value, nameof(cleanupOptions));
            _getUtcNow = EnsureArg.IsNotNull(getDateTimeUtcNow, nameof(getDateTimeUtcNow));
        }

        [FunctionName(nameof(PurgeOrchestrationInstanceHistory))]
        public async Task Run(
            [TimerTrigger(PurgeFrequencyVariable)] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log,
            CancellationToken hostCancellationToken)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(myTimer, nameof(myTimer));
            EnsureArg.IsNotNull(log, nameof(log));

            var orchestrationInstanceIdList = new List<string>();

            log.LogInformation("Purging orchestration instance history at: {Timestamp}", _getUtcNow());
            if (myTimer.IsPastDue)
            {
                log.LogWarning("Current function invocation is later than scheduled.");
            }

            // Specify conditions for orchestration instances.
            var condition = new OrchestrationStatusQueryCondition
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
                log.LogDebug("List of cleaned instance IDs: {list}", string.Join(", ", orchestrationInstanceIdList));
            }
            else
            {
                log.LogInformation("No Orchestration instances found within given conditions.");
            }
        }
    }
}
