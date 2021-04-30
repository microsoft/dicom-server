// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Health.Dicom.Core.Features.HealthCheck
{
    public class BackgroundServiceHealthCheckCache
    {
        private IMemoryCache _cache;

        private const string OldestDeleteInstanceCacheKey = "_oldestDeleted";

        private const string NumDeleteMaxRetryCacheKey = "_numMaxRetries";

        public BackgroundServiceHealthCheckCache(IMemoryCache memoryCache)
        {
            EnsureArg.IsNotNull(memoryCache, nameof(memoryCache));

            _cache = memoryCache;
        }

        public Task<DateTimeOffset> GetOrAddOldestTimeAsync(Func<CancellationToken, Task<DateTimeOffset>> getOldestTime, CancellationToken cancellationToken)
        {
            return _cache.GetOrCreateAsync(OldestDeleteInstanceCacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return getOldestTime(cancellationToken);
            });
        }

        public Task<int> GetOrAddNumExhaustedDeletionAttemptsAsync(Func<CancellationToken, Task<int>> getRetries, CancellationToken cancellationToken)
        {
            return _cache.GetOrCreateAsync(NumDeleteMaxRetryCacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return getRetries(cancellationToken);
            });
        }
    }
}
