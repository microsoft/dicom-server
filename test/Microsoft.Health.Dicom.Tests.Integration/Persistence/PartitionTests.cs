// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class PartitionTests : IClassFixture<SqlDataStoreTestsFixture>
{
    private readonly SqlDataStoreTestsFixture _fixture;

    public PartitionTests(SqlDataStoreTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenNewPartitionIsCreated_Then_ItIsRetrievable()
    {
        string partitionName = "test";

        await _fixture.PartitionStore.AddPartitionAsync(partitionName);
        Partition partition = await _fixture.PartitionStore.GetPartitionAsync(partitionName);

        Assert.NotNull(partition);
    }

    [Fact]
    public async Task WhenTwoNewPartitionIsCreatedWithSame_Then_ItThrowsException()
    {
        string partitionName = new Guid().ToString("N");

        await _fixture.PartitionStore.AddPartitionAsync(partitionName);
        Partition partition = await _fixture.PartitionStore.GetPartitionAsync(partitionName);

        Assert.NotNull(partition);

        await Assert.ThrowsAsync<DataPartitionAlreadyExistsException>(() => _fixture.PartitionStore.AddPartitionAsync(partitionName));
    }

    [Fact]
    public async Task WhenGetPartitionsIsCalled_Then_DefaultPartitionRecordIsReturned()
    {
        IEnumerable<Partition> partitions = await _fixture.PartitionStore.GetPartitionsAsync();

        Assert.Contains(partitions, p => p.Key == Partition.DefaultKey);
    }

    [Fact]
    public async Task WhenGetPartitionIsCalledWithDefaultPartitionName_Then_DefaultPartitionRecordIsReturned()
    {
        Partition partition = await _fixture.PartitionStore.GetPartitionAsync(Partition.DefaultName);

        Assert.Equal(Partition.DefaultKey, partition.Key);
    }

    [Fact]
    public async Task WhenNewPartitionIsCreatedInParallelWithSame_Then_ItThrowsException()
    {
        string partitionName = Guid.NewGuid().ToString("N");

        await Assert.ThrowsAsync<DataPartitionAlreadyExistsException>(() => Task.WhenAll(
                       _fixture.PartitionStore.AddPartitionAsync(partitionName),
                       _fixture.PartitionStore.AddPartitionAsync(partitionName)));
    }
}
