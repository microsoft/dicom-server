// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Messages.Partitioning;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Partitioning;

public class PartitionServiceTests
{
    private readonly IOptions<DataPartitionConfiguration> _partitionCacheOptions;
    private readonly IPartitionStore _partitionStore;
    private readonly PartitionCache _partitionCache;
    private readonly PartitionService _partitionService;

    public PartitionServiceTests()
    {
        _partitionCacheOptions = Substitute.For<IOptions<DataPartitionConfiguration>>();
        _partitionCacheOptions.Value.Returns(new DataPartitionConfiguration());
        _partitionStore = Substitute.For<IPartitionStore>();
        _partitionCache = new PartitionCache(_partitionCacheOptions, new LoggerFactory(), NullLogger<PartitionCache>.Instance);
        _partitionService = new PartitionService(_partitionCache, _partitionStore, NullLogger<PartitionService>.Instance);
    }

    [Fact]
    public async Task GivenAGetOrAddRequest_WhenPartitionExists_ReturnsPartition()
    {
        var returnThis = new Partition(1, "test", DateTimeOffset.Now);
        _partitionStore.GetPartitionAsync("test", Arg.Any<CancellationToken>()).Returns(returnThis);

        GetOrAddPartitionResponse result = await _partitionService.GetOrAddPartitionAsync("test", CancellationToken.None);

        Assert.Equal("test", result.Partition.Name);
        Assert.Equal(1, result.Partition.Key);

        await _partitionStore.DidNotReceiveWithAnyArgs().AddPartitionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenANonExistingPartition_WhenAttemptingToGet_ThrowsDataPartitionNotFound()
    {
        _partitionStore.GetPartitionAsync("notfound", CancellationToken.None).Returns((Partition)null);

        await Assert.ThrowsAsync<DataPartitionsNotFoundException>(() => _partitionService.GetPartitionAsync("notfound", CancellationToken.None));
    }

    [Fact]
    public async Task GivenAnInvalidPartition_WhenAttemptingToGet_ThrowsInvalidPartition()
    {
        await Assert.ThrowsAsync<InvalidPartitionNameException>(() => _partitionService.GetPartitionAsync("test#$", CancellationToken.None));
    }

    [Fact]
    public async Task GivenAnInvalidPartition_WhenAttemptingToGetOrAdd_ThrowsInvalidPartition()
    {
        await Assert.ThrowsAsync<InvalidPartitionNameException>(() => _partitionService.GetOrAddPartitionAsync("test#$", CancellationToken.None));
    }

    [Fact]
    public async Task GivenAGetOrAddRequest_WhenPartitionDoesntExist_CreatesAndReturnsPartition()
    {
        var returnThis = new Partition(1, "test", DateTimeOffset.Now);
        _partitionStore.GetPartitionAsync("test", Arg.Any<CancellationToken>()).Returns((Partition)null);
        _partitionStore.AddPartitionAsync("test", Arg.Any<CancellationToken>()).Returns(returnThis);

        GetOrAddPartitionResponse result = await _partitionService.GetOrAddPartitionAsync("test", CancellationToken.None);

        Assert.Equal("test", result.Partition.Name);
        Assert.Equal(1, result.Partition.Key);

        await _partitionStore.Received(1).AddPartitionAsync("test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenAGetOrAddRequest_WhenPartitionCreatedInMeantime_ReturnsPartition()
    {
        var returnThis = new Partition(1, "test", DateTimeOffset.Now);
        _partitionStore.GetPartitionAsync("test", Arg.Any<CancellationToken>())
            .Returns(_ => null, _ => returnThis);
        _partitionStore.AddPartitionAsync("test", Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(new DataPartitionAlreadyExistsException());

        GetOrAddPartitionResponse result = await _partitionService.GetOrAddPartitionAsync("test", CancellationToken.None);

        Assert.Equal("test", result.Partition.Name);
        Assert.Equal(1, result.Partition.Key);
    }
}
