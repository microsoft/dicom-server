// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenTagsInMultipleStatus_WhenGetProcessingQueryTagsActivityAsync_ShouldOnlyReturnProcessingTags()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagStoreEntry storeEntry1 = tag1.BuildExtendedQueryTagStoreEntry(key: 1);
            var expectedReturn = new[] { storeEntry1 };

            string operationId = Guid.NewGuid().ToString();
            ReindexEntry entry1 = new ReindexEntry() { StartWatermark = 0, EndWatermark = 1, OperationId = operationId, Status = IndexStatus.Processing, TagKey = 1 };
            ReindexEntry entry2 = new ReindexEntry() { StartWatermark = 0, EndWatermark = 1, OperationId = operationId, Status = IndexStatus.Completed, TagKey = 2 };
            ReindexEntry entry3 = new ReindexEntry() { StartWatermark = 0, EndWatermark = 1, OperationId = operationId, Status = IndexStatus.Paused, TagKey = 3 };
            _reindexStore.GetReindexEntriesAsync(operationId, Arg.Any<CancellationToken>()).Returns(new[] { entry1, entry2, entry3 });
            _extendedQueryTagStore.GetExtendedQueryTagsByKeyAsync(Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(new int[] { 1 })), Arg.Any<CancellationToken>()).Returns(expectedReturn);
            var result = await _reindexDurableFunction.GetProcessingTagsAsync(operationId, NullLogger.Instance);
            Assert.Equal(result, expectedReturn);
        }

        [Fact]
        public async Task GivenWatermarkRange_WhenReindexInstanceActivityAsync_ShouldReindexAllWatermarks()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagStoreEntry storeEntry = tag.BuildExtendedQueryTagStoreEntry(key: 1);
            IReadOnlyList<ExtendedQueryTagStoreEntry> tagsEntries = new[] { storeEntry };

            ReindexInstanceInput input = new ReindexInstanceInput() { TagStoreEntries = tagsEntries, StartWatermark = 1, EndWatermark = 4 };
            VersionedInstanceIdentifier[] identifiers = new VersionedInstanceIdentifier[]
            {
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 4),
            };
            _instanceStore.GetInstanceIdentifiersAsync(input.StartWatermark, input.EndWatermark, Arg.Any<CancellationToken>()).Returns(identifiers);
            await _reindexDurableFunction.ReindexInstancesAsync(input, NullLogger.Instance);
            foreach (var identifier in identifiers)
            {
                await _instanceReindexer.Received().ReindexInstanceAsync(tagsEntries, identifier.Version, Arg.Any<CancellationToken>());
            }
        }
    }
}
