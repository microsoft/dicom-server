// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
{
    public class PartitionCache
    {
        private readonly MemoryCache _partitionCache;
        private const int MaxCachedPartitionEntries = 10000;

        public PartitionCache()
            : this(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = MaxCachedPartitionEntries
            }))
        {
        }

        internal PartitionCache(MemoryCache memoryCache)
        {
            EnsureArg.IsNotNull(memoryCache, nameof(memoryCache));

            _partitionCache = memoryCache;
        }

        public Task<PartitionEntry> GetOrAddPartitionAsync(Func<string, CancellationToken, Task<PartitionEntry>> getPartitionEntry, string partitionName, CancellationToken cancellationToken)
        {
            return _partitionCache.GetOrCreateAsync(partitionName, entry =>
            {
                entry.Size = 1;
                return getPartitionEntry(partitionName, cancellationToken);
            });
        }
    }
}
