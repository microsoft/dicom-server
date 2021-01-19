// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class InstanceIndexerTests
    {
        private IInstanceIndexer _instanceIndexer;
        private IMetadataStore _metadataStore;
        private ICustomTagIndexService _customTagIndexService;

        public InstanceIndexerTests()
        {
            _metadataStore = Substitute.For<IMetadataStore>();
            _customTagIndexService = Substitute.For<ICustomTagIndexService>();
            _instanceIndexer = new InstanceIndexer(_metadataStore, _customTagIndexService);
        }

        [Fact]
        public async Task GivenNoCustomTagIndexes_WhenIndex_ThenShouldNotCallUnderlyingService()
        {
            await _instanceIndexer.IndexInstanceAsync(new Dictionary<string, CustomTagStoreEntry>(), new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1));

            await _metadataStore.DidNotReceiveWithAnyArgs().GetInstanceMetadataAsync(default, default);
            await _customTagIndexService.DidNotReceiveWithAnyArgs().AddCustomTagIndexes(default, default, default);
        }

        [Fact]
        public async Task GivenCustomTagIndexes_WhenIndex_ThenShouldCallUnderlyingService()
        {
            DicomTag testTag = DicomTag.DeviceSerialNumber;
            DicomLongString testTagElement = new DicomLongString(testTag, "TestDeviceSN");
            DicomDataset testDataset = new DicomDataset(testTagElement);
            CustomTagStoreEntry testEntry = testTag.BuildCustomTagStoreEntry();
            _metadataStore.GetInstanceMetadataAsync(default, default).ReturnsForAnyArgs(testDataset);
            await _instanceIndexer.IndexInstanceAsync(new CustomTagStoreEntry[] { testEntry }.ToTagPathDictionary(), new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), version: 1));

            await _customTagIndexService.ReceivedWithAnyArgs(1).AddCustomTagIndexes(default, default, default);
        }
    }
}
