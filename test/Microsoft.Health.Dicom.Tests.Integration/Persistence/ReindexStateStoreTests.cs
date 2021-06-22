// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for ExtendedQueryTagStore
    /// </summary>
    public class ReindexStateStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IReindexStateStore _reindexStateStore;
        private readonly IReindexStateStoreTestHelper _reindexStateStoreTestHelper;
        private readonly IExtendedQueryTagStoreTestHelper _extendedQueryTagStoreTestHelper;
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;
        public ReindexStateStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreFactory, nameof(fixture.ExtendedQueryTagStoreFactory));
            EnsureArg.IsNotNull(fixture.ReindexStateStore, nameof(fixture.ReindexStateStore));
            EnsureArg.IsNotNull(fixture.IndexDataStoreFactory, nameof(fixture.IndexDataStoreFactory));

            _extendedQueryTagStoreFactory = fixture.ExtendedQueryTagStoreFactory;
            _reindexStateStore = fixture.ReindexStateStore;
            _reindexStateStoreTestHelper = fixture.ReindexStateStoreTestHelper;
            _extendedQueryTagStoreTestHelper = fixture.ExtendedQueryTagStoreTestHelper;
            _indexDataStoreFactory = fixture.IndexDataStoreFactory;
        }

        [Fact]
        public async Task GivenInvalidExtendedQueryTags_WhenPrepareReindex_ThenShouldThrowException()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ExtendedQueryTagStatus.Adding, 128);
            var storeEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync();
            string operationId = Guid.NewGuid().ToString();
            List<int> tagKeys = storeEntries.Select(x => x.Key).ToList();
            // TagKey -1 is invalid
            tagKeys.Add(-1);
            await Assert.ThrowsAsync<ExtendedQueryTagBusyException>(() => _reindexStateStore.PrepareReindexingAsync(tagKeys, operationId));
        }

        [Fact]
        public async Task GivenNoDicomInstances_WhenPrepareReindex_ThenWatermarkShouldBeNull()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();
            await extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ExtendedQueryTagStatus.Adding, 128);
            var storeEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync();
            string operationId = Guid.NewGuid().ToString();
            List<int> tagKeys = storeEntries.Select(x => x.Key).ToList();
            ReindexOperation reindexOperation = await _reindexStateStore.PrepareReindexingAsync(tagKeys, operationId);
            Assert.Null(reindexOperation.StartWatermark);
            Assert.Null(reindexOperation.EndWatermark);
        }

        [Fact]
        public async Task GivenValidExtendedQueryTags_WhenPrepareReindex_ThenExpectedRowsShouldBeenAdded()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            AddExtendedQueryTagEntry extendedQueryTagEntry1 = tag1.BuildAddExtendedQueryTagEntry();
            AddExtendedQueryTagEntry extendedQueryTagEntry2 = tag2.BuildAddExtendedQueryTagEntry(vr: DicomVRCode.CS);
            IExtendedQueryTagStore extendedQueryTagStore = await _extendedQueryTagStoreFactory.GetInstanceAsync();

            // Add instance
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            long watermark = await (await _indexDataStoreFactory.GetInstanceAsync()).CreateInstanceIndexAsync(dataset);

            await extendedQueryTagStore.AddExtendedQueryTagsAsync(new AddExtendedQueryTagEntry[] { extendedQueryTagEntry1, extendedQueryTagEntry2 }, ExtendedQueryTagStatus.Adding, 128);
            var storeEntries = await extendedQueryTagStore.GetExtendedQueryTagsAsync();
            string operationId = Guid.NewGuid().ToString();
            List<int> tagKeys = storeEntries.Select(x => x.Key).ToList();
            ReindexOperation reindexOperation = await _reindexStateStore.PrepareReindexingAsync(tagKeys, operationId);
            Assert.Equal(watermark, reindexOperation.StartWatermark);
            Assert.Equal(watermark, reindexOperation.EndWatermark);
            Assert.Equal(operationId, reindexOperation.OperationId);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _reindexStateStoreTestHelper.CleanupAsync();
            await _extendedQueryTagStoreTestHelper.CleanupAsync();
        }
    }
}
