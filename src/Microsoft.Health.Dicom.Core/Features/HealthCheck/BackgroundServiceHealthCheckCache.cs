// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;


namespace Microsoft.Health.Dicom.Core.Features.HealthCheck
{

    public class BackgroundServiceHealthCheckCache
    {
        private IMemoryCache _cache;

        public BackgroundServiceHealthCheckCache(IMemoryCache memoryCache)
        {
            EnsureArg.IsNotNull(memoryCache, nameof(memoryCache));

            _cache = memoryCache;
        }
        
        public int getNumRetries(string cacheKey)
        {
            if (_cache.TryGetValue(cacheKey, out int val))
            {
                return val;
            }

            return -1;  
        }

        public DateTimeOffset getOldestTime(string cacheKey)
        {
            if (_cache.TryGetValue(cacheKey, out DateTimeOffset val))
            {
                return val;
            }

            return new DateTimeOffset();
        }

        public T updateCache<T>(string cacheKey, T newValue)
        {
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return newValue;
            });
        }
    }
}
