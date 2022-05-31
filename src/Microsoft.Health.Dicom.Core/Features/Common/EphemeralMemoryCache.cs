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

namespace Microsoft.Health.Dicom.Core.Features.Common;

/// <summary>
/// Cache that stores a limited number of items for a limited amount of time.
/// </summary>
/// <typeparam name="TIn"></typeparam>
/// <typeparam name="TOut"></typeparam>
internal class EphemeralMemoryCache<TIn, TOut> : IDisposable
{
    private readonly CacheConfiguration _configuration;
    private readonly MemoryCache _memoryCache;
    private readonly ILogger<EphemeralMemoryCache<TIn, TOut>> _logger;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public EphemeralMemoryCache(
        IOptionsSnapshot<CacheConfiguration> configuration,
        ILoggerFactory loggerFactory,
        ILogger<EphemeralMemoryCache<TIn, TOut>> logger)
    {
        EnsureArg.IsNotNull(configuration?.Value, nameof(configuration));
        EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _configuration = configuration.Value;
        _semaphore = new SemaphoreSlim(1, 1);
        _memoryCache = new MemoryCache(
            new MemoryCacheOptions
            {
                SizeLimit = _configuration.MaxCacheSize,
            },
            loggerFactory);
        _logger = logger;
    }

    /// <summary>
    /// Gets the cached value. If the asyncFactory returns null, the value returned is null.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="input"></param>
    /// <param name="asyncFactory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TOut> GetAsync(
        object key,
        TIn input,
        Func<TIn, CancellationToken, Task<TOut>> asyncFactory,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(key, nameof(key));
        EnsureArg.IsNotNull(asyncFactory, nameof(asyncFactory));

        if (_memoryCache.TryGetValue(key, out TOut result))
        {
            return result;
        }

        // ideally we should lock the row that needs to be initialized.
        // but compromising over multiple thread initializing the same row.
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue(key, out result))
            {
                return result;
            }

            _logger.LogInformation("Cache miss. Initializing the row.");

            result = await asyncFactory(input, cancellationToken);

            // MemoryCache class does not allow null as a value
            if (result == null)
            {
                return result;
            }

            _memoryCache.Set(
                key,
                result,
                new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = new TimeSpan(0, _configuration.MaxCacheAbsoluteExpirationInMinutes, 0)
                });
        }
        finally
        {
            _semaphore.Release();
        }

        return result;
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
                _memoryCache.Dispose();
                _semaphore.Dispose();
            }

            _disposed = true;
        }
    }
}
