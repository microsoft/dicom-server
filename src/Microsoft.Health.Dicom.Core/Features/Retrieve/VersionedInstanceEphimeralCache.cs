// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class VersionedInstanceEphimeralCache : IDisposable, IVersionedInstanceEphimeralCache
    {

        private readonly MemoryCache _memoryCache;
        private readonly ILogger<VersionedInstanceEphimeralCache> _logger;
        private bool _disposed;

        public VersionedInstanceEphimeralCache(ILoggerFactory loggerFactory, ILogger<VersionedInstanceEphimeralCache> logger)
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

        public async Task<VersionedInstanceIdentifier> GetInstanceAsync(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            Func<string, string, string, CancellationToken, Task<IEnumerable<VersionedInstanceIdentifier>>> getInstanceFunc,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(getInstanceFunc, nameof(getInstanceFunc));

            var stopWatch = Stopwatch.StartNew();
            // todo semphore lock and allow 1
            VersionedInstanceIdentifier instanceIdentifier;
            string key = GenerateKey(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            if (!_memoryCache.TryGetValue(key, out instanceIdentifier))
            {
                var instanceIdentifiers = await getInstanceFunc(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);

                if (!instanceIdentifiers.Any())
                {
                    throw new InstanceNotFoundException();
                }

                instanceIdentifier = instanceIdentifiers.First();
                _memoryCache.Set<VersionedInstanceIdentifier>(
                    key,
                    instanceIdentifiers.First(),
                    new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = new TimeSpan(0, 5, 0) });
            }

            stopWatch.Stop();
            _logger.LogInformation("VersionedInstanceEphimeralCache:GetInstanceAsync: {0}", stopWatch.ElapsedMilliseconds);
            return instanceIdentifier;
        }

        private static string GenerateKey(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid)
        {
            return $"{studyInstanceUid}/{seriesInstanceUid}/{sopInstanceUid}";
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
