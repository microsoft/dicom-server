// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Health.Dicom.Core.Features.HealthCheck;

public sealed class BackgroundServiceHealthCheckCache : IDisposable
{
    private readonly MemoryCache _cache;

    private const string OldestDeleteInstanceCacheKey = "_oldestDeleted";
    private const string NumDeleteMaxRetryCacheKey = "_numMaxRetries";

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MemoryCache disposed by BackgroundServiceHealthCheckCache")]
    public BackgroundServiceHealthCheckCache()
        : this(new MemoryCache(new MemoryCacheOptions()))
    {
    }

    internal BackgroundServiceHealthCheckCache(MemoryCache memoryCache)
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

    public void Dispose()
        => _cache.Dispose();
}
