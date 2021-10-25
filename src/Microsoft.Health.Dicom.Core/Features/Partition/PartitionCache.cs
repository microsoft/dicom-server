// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
{
    public class PartitionCache : IDisposable
    {
        private readonly MemoryCache _partitionCache;
        private readonly ILogger<PartitionCache> _logger;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;
        private volatile object _initializing;

        private const int MaxCachedPartitionEntries = 10000;

        public PartitionCache(ILogger<PartitionCache> logger)
            : this(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = MaxCachedPartitionEntries
            }))
        {
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _initializing = _semaphore;
        }

        internal PartitionCache(MemoryCache memoryCache)
        {
            EnsureArg.IsNotNull(memoryCache, nameof(memoryCache));

            _partitionCache = memoryCache;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        public async Task<PartitionEntry> GetOrAddPartitionAsync(Func<string, CancellationToken, Task<PartitionEntry>> action, string partitionName, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(action, nameof(action));

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PartitionCache));
            }

            if (_partitionCache.TryGetValue<PartitionEntry>(partitionName, out var partitionEntry))
            {
                return partitionEntry;
            }

            if (_initializing is not null)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
#pragma warning disable CA1508
                    // Another thread may have already gone through this block
                    if (_initializing is not null)
#pragma warning restore CA1508
                    {
                        _logger.LogInformation("Partition with name '{partitionName}' not found in cache", partitionName);

                        partitionEntry = await action(partitionName, cancellationToken);

                        if (partitionEntry != null)
                        {
                            _partitionCache.Set(partitionName, partitionEntry);
                        }

                        _initializing = null; // Volatile write must occur after _value
                        return partitionEntry;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return partitionEntry;
        }
    }
}
