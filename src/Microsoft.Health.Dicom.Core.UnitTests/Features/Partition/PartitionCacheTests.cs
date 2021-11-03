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
using Microsoft.Health.Dicom.Core.Features.Partition;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Partition
{
    public class PartitionCacheTests
    {
        [Fact]
        public async Task GivenMultipleThreadsExecuteGetOrAddPartitionAsync_OnlyOnceActionShouldExecute()
        {
            var config = Substitute.For<IOptions<DataPartitionConfiguration>>();
            config.Value.Returns(new DataPartitionConfiguration());

            var logger = Substitute.For<ILogger<PartitionCache>>();
            var partitionCache = new PartitionCache(config, logger);

            int numExecuted = 0;

            Func<string, CancellationToken, Task<PartitionEntry>> mockAction = async (string partitionName, CancellationToken cancellationToken) =>
            {
                await Task.Delay(200, cancellationToken);
                numExecuted++;
                return new PartitionEntry(1, partitionName);
            };

            var threadList = Enumerable.Range(0, 5).Select(async _ => await partitionCache.GetOrAddPartitionAsync(mockAction, "", CancellationToken.None)).ToList();

            await Task.WhenAll(threadList);

            Assert.Equal(1, numExecuted);
        }
    }
}
