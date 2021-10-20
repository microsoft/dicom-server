// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
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

            await _fixture.PartitionStore.AddPartition(partitionName);
            PartitionEntry partition = await _fixture.PartitionStore.GetPartition(partitionName);

            Assert.NotNull(partition);
        }

        [Fact]
        public async Task WhenGetPartitionsIsCalled_Then_DefaultPartitionRecordIsReturned()
        {
            IEnumerable<PartitionEntry> partitionEntries = await _fixture.PartitionStore.GetPartitions();

            Assert.Contains(partitionEntries, p => p.PartitionKey == DefaultPartition.Key);
        }

        [Fact]
        public async Task WhenGetPartitionIsCalledWithDefaultPartitionName_Then_DefaultPartitionRecordIsReturned()
        {
            PartitionEntry partitionEntry = await _fixture.PartitionStore.GetPartition(DefaultPartition.Name);

            Assert.Equal(DefaultPartition.Key, partitionEntry.PartitionKey);
        }
    }
}
