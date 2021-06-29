// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
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
        public async Task GivenWatermarkRange_WhenReindexInstanceActivityAsync_ShouldReindexAllWatermarks()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            ExtendedQueryTagStoreEntry storeEntry = tag.BuildExtendedQueryTagStoreEntry(key: 1);
            IReadOnlyList<ExtendedQueryTagStoreEntry> tagsEntries = new[] { storeEntry };

            ReindexInstanceInput input = new ReindexInstanceInput() { TagStoreEntries = tagsEntries, WatermarkRange = new WatermarkRange(1, 4) };
            VersionedInstanceIdentifier[] identifiers = new VersionedInstanceIdentifier[]
            {
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 4),
            };
            _instanceStore.GetInstanceIdentifiersAsync(input.WatermarkRange, Arg.Any<CancellationToken>()).Returns(identifiers);
            await _reindexDurableFunction.ReindexInstancesAsync(input, NullLogger.Instance);
            foreach (var identifier in identifiers)
            {
                await _instanceReindexer.Received().ReindexInstanceAsync(tagsEntries, identifier.Version, Arg.Any<CancellationToken>());
            }
        }
    }
}
