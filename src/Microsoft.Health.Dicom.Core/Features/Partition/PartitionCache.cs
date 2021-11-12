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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Core.Features.Partition
{
    public class PartitionCache : IDisposable
    {
        private readonly MemoryCache _partitionCache;
        private readonly ILogger<PartitionCache> _logger;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;
        private readonly DataPartitionConfiguration _configuration;

        public PartitionCache(IOptions<DataPartitionConfiguration> configuration, ILogger<PartitionCache> logger)
        {
            EnsureArg.IsNotNull(configuration?.Value);

            _partitionCache = new MemoryCache(
                new MemoryCacheOptions
                {
                    SizeLimit = configuration.Value.MaxCacheSize
                });

            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _configuration = configuration.Value;
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
                    _partitionCache.Dispose();
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

            await _semaphore.WaitAsync(cancellationToken);
            try
            {

                if (!_partitionCache.TryGetValue(partitionName, out PartitionEntry partitionEntry))
                {
                    _logger.LogInformation("Partition with name '{PartitionName}' not found in cache", partitionName);

                    partitionEntry = await action(partitionName, cancellationToken);

                    if (partitionEntry != null)
                    {
                        _partitionCache.Set(partitionName, partitionEntry, new MemoryCacheEntryOptions
                        {
                            Size = 1,
                            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_configuration.MaxCacheAbsoluteExpirationInMinutes)
                        });
                    }
                }

                return partitionEntry;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
