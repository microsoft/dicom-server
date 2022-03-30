// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Dicom.Features.Common;

public class AsyncCache<T> : IDisposable
{
    private readonly Func<CancellationToken, Task<T>> _factory;
    private readonly SemaphoreSlim _semaphore;
    private volatile object _initializing;
    private bool _disposed;
    private T _value;

    public AsyncCache(Func<CancellationToken, Task<T>> factory)
    {
        _factory = EnsureArg.IsNotNull(factory, nameof(factory));
        _semaphore = new SemaphoreSlim(1, 1);

        // _initializing is a volatile reference type flag for determining whether init is needed.
        // It is arbitrarily assigned _semaphore instead of allocating a new object
        _initializing = _semaphore;
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

    public async Task<T> GetAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsyncCache<T>));
        }

        if (forceRefresh || _initializing is not null)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
#pragma warning disable CA1508
                // Another thread may have already gone through this block
                if (forceRefresh || _initializing is not null)
#pragma warning restore CA1508
                {
                    _value = await _factory(cancellationToken);
                    _initializing = null; // Volatile write must occur after _value
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return _value;
    }
}
