// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Features.HealthCheck;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.HealthCheck
{
    public class BackgroundServiceHealthCheckCacheTests
    {
        private BackgroundServiceHealthCheckCache _backgroundServiceHealthCheckCache;
        private IMemoryCache _cache;
        private DateTimeOffset _testDateTimeOffset = new DateTimeOffset();
        private int _testNumRetries = 0;

        public BackgroundServiceHealthCheckCacheTests()
        {
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();
            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            _cache = memoryCache;
            _backgroundServiceHealthCheckCache = new BackgroundServiceHealthCheckCache(memoryCache);
        }

        [Fact]
        public async Task IfNoChachedValueNumRetries_ReturnNewValue()
        {
            _testNumRetries = 5;
            int result = await _backgroundServiceHealthCheckCache.GetOrAddNumExhaustedDeletionAttemptsAsync(GetNumRetriesTest, CancellationToken.None);
            Assert.Equal(5, result);
            _cache.Remove("_numMaxRetries");
        }

        [Fact]
        public async Task IfChachedValueNumRetries_ReturCachedValue()
        {
            _testNumRetries = 10;
            int addFrist = await _backgroundServiceHealthCheckCache.GetOrAddNumExhaustedDeletionAttemptsAsync(GetNumRetriesTest, CancellationToken.None);
            _testNumRetries = 5;
            int result = await _backgroundServiceHealthCheckCache.GetOrAddNumExhaustedDeletionAttemptsAsync(GetNumRetriesTest, CancellationToken.None);

            Assert.Equal(10, result);
            _cache.Remove("_numMaxRetries");
        }

        [Fact]
        public async Task IfNoChachedValueOldestDate_ReturnNewValue()
        {
            _testDateTimeOffset = new DateTimeOffset(1000000, new TimeSpan(0));
            DateTimeOffset result = await _backgroundServiceHealthCheckCache.GetOrAddOldestTimeAsync(GetOldestTimeTest, CancellationToken.None);
            Assert.Equal(new DateTimeOffset(1000000, new TimeSpan(0)), result);
            _cache.Remove("_oldestDeleted");
        }

        [Fact]
        public async Task IfChachedValueOldestDate_ReturCachedValue()
        {
            _testDateTimeOffset = new DateTimeOffset(1000000, new TimeSpan(0));
            DateTimeOffset addFrist = await _backgroundServiceHealthCheckCache.GetOrAddOldestTimeAsync(GetOldestTimeTest, CancellationToken.None);
            _testDateTimeOffset = new DateTimeOffset(10000, new TimeSpan(0));
            DateTimeOffset result = await _backgroundServiceHealthCheckCache.GetOrAddOldestTimeAsync(GetOldestTimeTest, CancellationToken.None);

            Assert.Equal(new DateTimeOffset(1000000, new TimeSpan(0)), result);
            _cache.Remove("_oldestDeleted");
        }

        private Task<int> GetNumRetriesTest(CancellationToken token)
        {
            return Task.FromResult(_testNumRetries);
        }

        private Task<DateTimeOffset> GetOldestTimeTest(CancellationToken token)
        {
            return Task.FromResult(_testDateTimeOffset);
        }
    }
}
