// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class ReindexJob : IReindexJob
    {
        private readonly ICustomTagJobStore _customTagJobStore;
        private readonly ICustomTagStore _customTagStore;
        private readonly IInstanceIndexer _instanceIndexer;
        private readonly ILogger<ReindexJob> _logger;

        // Start from 1 for now, we can consider bigger number for better performance later.
        private const int Top = 1;

        public ReindexJob(ICustomTagJobStore customTagJobStore, ICustomTagStore customTagStore, IInstanceIndexer instanceIndexer, ILogger<ReindexJob> logger)
        {
            EnsureArg.IsNotNull(customTagJobStore, nameof(customTagJobStore));
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(instanceIndexer, nameof(instanceIndexer));
            EnsureArg.IsNotNull(logger, nameof(logger));
            _customTagStore = customTagStore;
            _instanceIndexer = instanceIndexer;
            _customTagJobStore = customTagJobStore;
            _logger = logger;
        }

        public async Task ReindexAsync(long jobKey, CancellationToken cancellationToken)
        {
            try
            {
                CustomTagJob job = await _customTagJobStore.GetCustomTagJobAsync(jobKey, cancellationToken);
                if (job.Type != CustomTagJobType.Reindexing)
                {
                    throw new NotSupportedException("Not reindexing job");
                }

                // The job should be acquired ealier, so status should be executing
                if (job.Status != CustomTagJobStatus.Executing)
                {
                    throw new NotSupportedException("Not executing");
                }

                // TODO: what if fail to get job information
                // TODO: what if this code is got executed long after GetCustomTagJobAsync where this job has been picked up by another worker?
                IEnumerable<CustomTagStoreEntry> customTagStoreEntries = await _customTagJobStore.GetCustomTagsOnJobAsync(jobKey, cancellationToken);

                if (customTagStoreEntries.Count() == 0)
                {
                    // update job status
                    _logger.LogInformation($"No customtags is assoiated with job {jobKey}, job is completing");
                }
                else
                {
                    Dictionary<string, CustomTagStoreEntry> tagPathDictionary = customTagStoreEntries.ToDictionary(
                           keySelector: entry => entry.Path,
                           comparer: StringComparer.OrdinalIgnoreCase);

                    // if completedWatermark specified, means the job was processed in the past.
                    long maxWatermark = job.CompletedWatermark.HasValue ? job.CompletedWatermark.Value - 1 : job.MaxWatermark;
                    while (true)
                    {
                        IEnumerable<VersionedInstanceIdentifier> instances = await _customTagStore.GetInstancesInThePastAsync(maxWatermark, top: Top, indexStatus: IndexStatus.Created, cancellationToken);
                        if (instances.Count() == 0)
                        {
                            break;
                        }

                        instances = instances.OrderByDescending(item => item.Version);

                        // Please note that, if reindexing any instances fails in IndexInstances, the excution throws exception and stop.
                        // Then resuming job will reindex these instances again even they have been reindexed already.
                        IndexInstances(instances, tagPathDictionary, cancellationToken);

                        maxWatermark = instances.Last().Version - 1;

                        // update completed watermark
                        await _customTagJobStore.UpdateCustomTagJobCompletedWatermarkAsync(jobKey, maxWatermark, cancellationToken);
                    }

                    _logger.LogInformation($"Job {jobKey} is completed.");
                }

                // update job satus
                await _customTagJobStore.RemoveCustomTagJobAsync(jobKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fail to reindex job for exception {ex}");

                // if fail to update jobstatus, the job will be on Excuting status until timeout, then get pickup and executed by another worker
                await _customTagJobStore.UpdateCustomTagJobStatusAsync(jobKey, CustomTagJobStatus.Error);
            }
        }

        private void IndexInstances(IEnumerable<VersionedInstanceIdentifier> instances, Dictionary<string, CustomTagStoreEntry> tagPathDictionary, CancellationToken cancellationToken)
        {
            Task[] tasks = new Task[instances.Count()];
            int taskIndex = 0;
            foreach (var instance in instances)
            {
                tasks[taskIndex] = _instanceIndexer.IndexInstanceAsync(tagPathDictionary, instance, cancellationToken);
                taskIndex++;
            }

            Task.WaitAll(tasks, cancellationToken);
        }
    }
}
