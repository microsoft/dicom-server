// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

        public async Task ReindexAsync(IEnumerable<CustomTagEntry> customTags, long endWatermark, CancellationToken cancellationToken)
        {
            while (true)
            {
                IEnumerable<VersionedInstanceIdentifier> instances = await _customTagStore.GetEarilierInstancesAsync(endWatermark, top: Top, indexStatus: IndexStatus.Created, cancellationToken);
                if (instances.Count() == 0)
                {
                    break;
                }

                instances = instances.OrderByDescending(item => item.Version);
                foreach (var instance in instances)
                {
                    // 
                    await _instanceIndexer.IndexInstanceAsync(customTags, instance, cancellationToken);
                }

                // TODO:  Once we have job framework, job status should be saved and kept updating
                endWatermark = instances.Last().Version - 1;
            }
        }
    }
}
