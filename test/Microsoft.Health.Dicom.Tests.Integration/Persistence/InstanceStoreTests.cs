// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    ///  Tests for InstanceStore.
    /// </summary>
    public partial class InstanceStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly IStoreFactory<IInstanceStore> _instanceStoreFactory;
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;

        public InstanceStoreTests(SqlDataStoreTestsFixture fixture)
        {
            _instanceStoreFactory = EnsureArg.IsNotNull(fixture?.InstanceStoreFactory, nameof(fixture.InstanceStoreFactory));
            _indexDataStoreFactory = EnsureArg.IsNotNull(fixture?.IndexDataStoreFactory, nameof(fixture.IndexDataStoreFactory));
        }

        [Fact]
        public async Task GivenInstances_WhenGetInstanceIdentifiersByWatermarkRange_ThenItShouldReturnInstancesInRange()
        {
            await AddRandomInstanceAsync();
            var instance1 = await AddRandomInstanceAsync();
            var instance2 = await AddRandomInstanceAsync();
            var instance3 = await AddRandomInstanceAsync();
            var instance4 = await AddRandomInstanceAsync();
            await AddRandomInstanceAsync();

            IInstanceStore instanceStore = await _instanceStoreFactory.GetInstanceAsync();
            IReadOnlyList<VersionedInstanceIdentifier> instances = await instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(
                WatermarkRange.Between(instance1.Version, instance4.Version),
                IndexStatus.Creating);

            Assert.Equal(instances, new[] { instance1, instance2, instance3 });
        }

        [Fact]
        public async Task GivenInstances_WhenGettingMaxInstanceWatermark_ThenReturnMaxValue()
        {
            IInstanceStore instanceStore = await _instanceStoreFactory.GetInstanceAsync();

            // TODO: How to check empty?

            // Populate DB and Check
            await AddRandomInstanceAsync();
            await AddRandomInstanceAsync();
            await AddRandomInstanceAsync();
            var last = await AddRandomInstanceAsync();

            Assert.Equal(last.Version, await instanceStore.GetMaxInstanceWatermarkAsync());
        }

        private async Task<VersionedInstanceIdentifier> AddRandomInstanceAsync()
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);

            IIndexDataStore indexDataStore = await _indexDataStoreFactory.GetInstanceAsync();
            long version = await indexDataStore.CreateInstanceIndexAsync(dataset);
            return new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);
        }
    }
}
