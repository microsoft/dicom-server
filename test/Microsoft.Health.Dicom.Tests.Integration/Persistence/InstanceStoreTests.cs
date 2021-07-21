// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using EnsureThat;
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
        private readonly IInstanceStore _instanceStore;
        private readonly IIndexDataStore _indexDataStore;

        public InstanceStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture?.InstanceStore, nameof(fixture.InstanceStore));
            EnsureArg.IsNotNull(fixture?.IndexDataStore, nameof(fixture.IndexDataStore));
            _instanceStore = fixture.InstanceStore;
            _indexDataStore = fixture.IndexDataStore;
        }

        [Fact]
        public async Task GivenInstances_WhenGetInstanceIdentifiersByWatermarkRange_ThenItShouldReturnInstancesInRange()
        {
            var instance0 = await AddRandomInstanceAsync();
            var instance1 = await AddRandomInstanceAsync();
            var instance2 = await AddRandomInstanceAsync();
            var instance3 = await AddRandomInstanceAsync();
            var instance4 = await AddRandomInstanceAsync();
            var instances = await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(new WatermarkRange(instance1.Version, instance3.Version), IndexStatus.Creating);
            Assert.Equal(instances, new[] { instance1, instance2, instance3 });
        }

        private async Task<VersionedInstanceIdentifier> AddRandomInstanceAsync()
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);

            long version = await _indexDataStore.CreateInstanceIndexAsync(dataset);
            return new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);
        }

    }
}
