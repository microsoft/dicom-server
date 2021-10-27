// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class FramesRangeCache : IDisposable, IFramesRangeCache
    {

        private readonly MemoryCache _memoryCache;
        private readonly ILogger<FramesRangeCache> _logger;
        private bool _disposed;

        public FramesRangeCache(ILoggerFactory loggerFactory, ILogger<FramesRangeCache> logger)
        {
            EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            // TODO
            _memoryCache = new MemoryCache(
                new MemoryCacheOptions
                {
                    SizeLimit = 10000,
                },
                loggerFactory);
            _logger = logger;
        }

        public async Task<FrameRange> GetFrameRangeAsync(VersionedInstanceIdentifier identifier, int frame, Func<VersionedInstanceIdentifier, CancellationToken, Task<Dictionary<int, FrameRange>>> getFrameFunc, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(getFrameFunc, nameof(getFrameFunc));

            var stopWatch = Stopwatch.StartNew();
            // todo semphore lock and allow 1
            Dictionary<int, FrameRange> framesRange;
            if (!_memoryCache.TryGetValue(identifier, out framesRange))
            {
                framesRange = await getFrameFunc(identifier, cancellationToken);

                if (framesRange == null)
                {
                    return null;
                }
                _logger.LogInformation("FramesRangeCache cache miss for {0}", identifier);

                _memoryCache.Set<Dictionary<int, FrameRange>>(
                    identifier,
                    framesRange,
                    new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = new TimeSpan(1, 0, 0) });
            }

            stopWatch.Stop();
            _logger.LogInformation("FrameRangeCache:GetFrameRangeAsync: {0}", stopWatch.ElapsedMilliseconds);
            return framesRange.TryGetValue(frame, out FrameRange httpRange) == true ? httpRange : throw new FrameNotFoundException();
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
                }

                _disposed = true;
            }
        }
    }
}
