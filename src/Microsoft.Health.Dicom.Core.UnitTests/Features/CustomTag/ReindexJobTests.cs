// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class ReindexJobTests
    {
        private ICustomTagStore _customTagStore;
        private IInstanceIndexer _instanceIndexer;
        private IReindexJob _reindexJob;

        public ReindexJobTests()
        {
            _customTagStore = Substitute.For<ICustomTagStore>();
            _instanceIndexer = Substitute.For<IInstanceIndexer>();
            _reindexJob = new ReindexJob(_customTagStore, _instanceIndexer);
        }

        [Fact]
        public async Task GivenNoCustomTagEntries_WhenRedinex_ThenShouldNotCallUnderlyingService()
        {
            await _reindexJob.ReindexAsync(new CustomTagStoreEntry[0], 1);

            await _customTagStore.DidNotReceiveWithAnyArgs().GetInstancesInThePastAsync(0, 1);
        }

        [Fact]
        public async Task GivenNoInstancesReturned_WhenRedinex_ThenShouldNotCallUnderlyingService()
        {
            DicomTag testTag = DicomTag.DeviceSerialNumber;

            _customTagStore.GetInstancesInThePastAsync(1, 1).ReturnsForAnyArgs(new VersionedInstanceIdentifier[0]);

            await _reindexJob.ReindexAsync(new CustomTagStoreEntry[] { testTag.BuildCustomTagStoreEntry() }, 1);
            await _instanceIndexer.DidNotReceiveWithAnyArgs().IndexInstanceAsync(default, default);
        }

        [Fact]
        public async Task GivenMultipleInstances_WhenRedinex_ThenShouldProcessTillNoInstancesReturn()
        {
            DicomTag testTag = DicomTag.DeviceSerialNumber;
            VersionedInstanceIdentifier instanceId1 = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
            VersionedInstanceIdentifier instanceId2 = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 2);

            _customTagStore.GetInstancesInThePastAsync(instanceId2.Version, 1, Models.IndexStatus.Created, Arg.Any<CancellationToken>())
                .Returns(new VersionedInstanceIdentifier[] { instanceId2 });

            _customTagStore.GetInstancesInThePastAsync(instanceId1.Version, 1, Models.IndexStatus.Created, Arg.Any<CancellationToken>())
                .Returns(new VersionedInstanceIdentifier[] { instanceId1 });

            _customTagStore.GetInstancesInThePastAsync(instanceId1.Version - 1, 1, Models.IndexStatus.Created, Arg.Any<CancellationToken>())
                .Returns(new VersionedInstanceIdentifier[] { });

            await _reindexJob.ReindexAsync(new CustomTagStoreEntry[] { testTag.BuildCustomTagStoreEntry() }, instanceId2.Version);

            // all instances are indexed
            await _instanceIndexer.Received(1).IndexInstanceAsync(Arg.Any<Dictionary<string, CustomTagStoreEntry>>(), instanceId2, Arg.Any<CancellationToken>());
            await _instanceIndexer.Received(1).IndexInstanceAsync(Arg.Any<Dictionary<string, CustomTagStoreEntry>>(), instanceId1, Arg.Any<CancellationToken>());
        }
    }
}
