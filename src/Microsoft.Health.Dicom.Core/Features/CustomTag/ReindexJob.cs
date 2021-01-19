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
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class ReindexJob : IReindexJob
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IInstanceIndexer _instanceIndexer;

        // Start from 1 for now, we can consider bigger number for better performance later.
        private const int Top = 1;

        public ReindexJob(ICustomTagStore customTagStore, IInstanceIndexer instanceIndexer)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(instanceIndexer, nameof(instanceIndexer));
            _customTagStore = customTagStore;
            _instanceIndexer = instanceIndexer;
        }

        public async Task ReindexAsync(IEnumerable<CustomTagStoreEntry> customTagStoreEntries, long endWatermark, CancellationToken cancellationToken)
        {
            // TODO: this is transient solution for main scenario to go through. There are several things need to be done when move onto job framework.
            // 1. The reindex status (endWatermark) should be updated as reindexing
            // 2. job framework should allow resume, so that if reindex fail in the middle, user is able to resume.
            if (customTagStoreEntries.Count() == 0)
            {
                return;
            }

            Dictionary<string, CustomTagStoreEntry> tagPathDictionary = customTagStoreEntries.ToDictionary(
                   keySelector: entry => entry.Path,
                   comparer: StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                IEnumerable<VersionedInstanceIdentifier> instances = await _customTagStore.GetInstancesInThePastAsync(endWatermark, top: Top, indexStatus: IndexStatus.Created, cancellationToken);
                if (instances.Count() == 0)
                {
                    break;
                }

                instances = instances.OrderByDescending(item => item.Version);

                // Please note that, if reindexing any instances fails in IndexInstances, the excution throws exception and stop.
                // Then resuming job will reindex these instances again even they have been reindexed already.
                IndexInstances(instances, tagPathDictionary, cancellationToken);

                // TODO:  Once we have job framework, job status should be saved and kept updating
                endWatermark = instances.Last().Version - 1;
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
