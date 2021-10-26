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
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Partition;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Partition
{
    public class PartitionCacheTests
    {
        [Fact]
        public void GivenMultipleThreadsExecuteGetOrAddPartitionAsync_OnlyOnceActionShouldExecute()
        {
            var mockAction = Substitute.For<Func<string, CancellationToken, Task<PartitionEntry>>>();

            var config = Substitute.For<IOptions<DataPartitionConfiguration>>();
            config.Value.Returns(new DataPartitionConfiguration());

            var logger = Substitute.For<ILogger<PartitionCache>>();
            var partitionCache = new PartitionCache(config, logger);

            mockAction.When(x => x.Invoke(Arg.Any<string>(), Arg.Any<CancellationToken>())).Do(x => Task.Delay(200));

            var threadList = Enumerable.Range(0, 5).Select(_ => new Thread(async () => await partitionCache.GetOrAddPartitionAsync(mockAction, "", CancellationToken.None))).ToList();

            threadList.ForEach(x => x.Start());
            threadList.ForEach(x => x.Join());

            mockAction.Received(1)("", CancellationToken.None);
        }
    }
}
