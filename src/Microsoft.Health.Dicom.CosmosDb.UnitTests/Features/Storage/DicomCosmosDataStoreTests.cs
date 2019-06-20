// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.Health.CosmosDb.Configs;
using Microsoft.Health.Dicom.CosmosDb.Config;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage
{
    public class DicomCosmosDataStoreTests
    {
        private readonly DicomCosmosDataStore _indexDataStore;

        public DicomCosmosDataStoreTests()
        {
            IOptionsMonitor<CosmosCollectionConfiguration> namedCosmosCollectionConfigurationAccessor = Substitute.For<IOptionsMonitor<CosmosCollectionConfiguration>>();
            namedCosmosCollectionConfigurationAccessor.Get(Constants.CollectionConfigurationName).Returns(new CosmosCollectionConfiguration() { CollectionId = "testcollection" });

            _indexDataStore = new DicomCosmosDataStore(
                Substitute.For<IScoped<IDocumentClient>>(),
                new CosmosDataStoreConfiguration(),
                namedCosmosCollectionConfigurationAccessor,
                new DicomCosmosConfiguration());
        }

        [Fact]
        public async Task GivenInvalidParameters_WhenCallingAllMethods_ArgumentExceptionThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.IndexInstanceAsync(null));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.DeleteSeriesIndexAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.DeleteSeriesIndexAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.DeleteSeriesIndexAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.DeleteSeriesIndexAsync(Guid.NewGuid().ToString(), string.Empty));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.DeleteInstanceIndexAsync(null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.DeleteInstanceIndexAsync(string.Empty, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.DeleteInstanceIndexAsync(Guid.NewGuid().ToString(), null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.DeleteInstanceIndexAsync(Guid.NewGuid().ToString(), string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.DeleteInstanceIndexAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.DeleteInstanceIndexAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), string.Empty));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.GetInstancesInStudyAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.GetInstancesInStudyAsync(string.Empty));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.GetInstancesInSeriesAsync(null, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.GetInstancesInSeriesAsync(string.Empty, Guid.NewGuid().ToString()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _indexDataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), null));
            await Assert.ThrowsAsync<ArgumentException>(() => _indexDataStore.GetInstancesInSeriesAsync(Guid.NewGuid().ToString(), string.Empty));
        }
    }
}
