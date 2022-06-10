// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Common;
public class EphemeralMemoryCacheTests
{
    [Fact]
    public async Task GivenCacheImp_MultipleThreadsSameGet_GetFuncExecutedOnce()
    {
        var config = Substitute.For<IOptions<CacheConfiguration>>();
        config.Value.Returns(new CacheConfiguration() { MaxCacheSize = 10, MaxCacheAbsoluteExpirationInMinutes = 1 });
        var cache = new TestEphemeralMemoryCache(config, Substitute.For<ILoggerFactory>(), Substitute.For<ILogger<TestEphemeralMemoryCache>>());

        int numExecuted = 0;
        Func<int, CancellationToken, Task<int?>> mockAction = async (int input, CancellationToken cancellationToken) =>
        {
            await Task.Delay(10, cancellationToken);
            numExecuted++;
            return 1;
        };

        var threadList = Enumerable.Range(0, 3).Select(async _ => await cache.GetAsync(1, 1, mockAction, CancellationToken.None));
        await Task.WhenAll(threadList);

        Assert.Equal(1, numExecuted);
    }

    [Fact]
    public async Task GivenCacheImp_WithFuncResultNull_Throws()
    {
        var config = Substitute.For<IOptions<CacheConfiguration>>();
        config.Value.Returns(new CacheConfiguration());
        var cache = new TestEphemeralMemoryCache(config, Substitute.For<ILoggerFactory>(), Substitute.For<ILogger<TestEphemeralMemoryCache>>());

        Func<int, CancellationToken, Task<int?>> mockAction = async (int input, CancellationToken cancellationToken) =>
        {
            await Task.Run(() => 1);
            return null;
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetAsync(1, 1, mockAction, CancellationToken.None));
    }

    public class TestEphemeralMemoryCache : EphemeralMemoryCache<int, int?>
    {
        public TestEphemeralMemoryCache(IOptions<CacheConfiguration> configuration, ILoggerFactory loggerFactory, ILogger<TestEphemeralMemoryCache> logger)
            : base(configuration, loggerFactory, logger)
        {
        }
    }
}
