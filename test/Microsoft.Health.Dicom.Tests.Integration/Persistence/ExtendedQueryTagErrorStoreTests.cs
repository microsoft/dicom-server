// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ExtendedQueryTagErrorStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IIndexDataStoreTestHelper _testHelper;

        public ExtendedQueryTagErrorStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _extendedQueryTagStore = EnsureArg.IsNotNull(fixture.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            _extendedQueryTagErrorStore = EnsureArg.IsNotNull(fixture.ExtendedQueryTagErrorStore, nameof(fixture.ExtendedQueryTagErrorStore));
            _indexDataStore = EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
            _testHelper = EnsureArg.IsNotNull(fixture.TestHelper, nameof(fixture.TestHelper));
        }

        public async Task DisposeAsync()
        {
            await _testHelper.ClearExtendedQueryTagErrorTable();
        }

        [Fact]
        public async Task GivenValidExtendedQueryTagError_WhenAddExtendedQueryTagError_ThenTagErrorShouldBeAdded()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomTag tag = DicomTag.DeviceSerialNumber;
            long watermark = await AddInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            int tagKey = await AddTagAsync(tag);

            int outputTagKey = await _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                tagKey,
                3,
                watermark);

            Assert.Equal(outputTagKey, tagKey);

            var extendedQueryTagError = await _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tag.GetPath());

            Assert.Equal(extendedQueryTagError[0].StudyInstanceUid, studyInstanceUid);
            Assert.Equal(extendedQueryTagError[0].SeriesInstanceUid, seriesInstanceUid);
            Assert.Equal(extendedQueryTagError[0].SopInstanceUid, sopInstanceUid);
        }

        [Fact]
        public async Task GivenNonExistingQueryTag_WhenAddExtendedQueryTagError_ThenShouldThrowException()
        {
            var extendedQueryTag = await _extendedQueryTagStore.GetExtendedQueryTagsAsync();
            Assert.Equal(0, extendedQueryTag.Count);
            await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(
                () => _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(1, 1, 1));
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        private async Task<long> AddInstanceAsync(string studyId, string seriesId, string sopInstanceId)
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyId, seriesId, sopInstanceId);
            long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset);
            await _indexDataStore.UpdateInstanceIndexStatusAsync(new VersionedInstanceIdentifier(studyId, seriesId, sopInstanceId, watermark), Core.Models.IndexStatus.Created);
            return watermark;
        }

        private async Task<int> AddTagAsync(DicomTag tag)
        {
            AddExtendedQueryTagEntry extendedQueryTagEntry = tag.BuildAddExtendedQueryTagEntry();
            var list = await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry }, 128);
            return list[0];
        }
    }
}
