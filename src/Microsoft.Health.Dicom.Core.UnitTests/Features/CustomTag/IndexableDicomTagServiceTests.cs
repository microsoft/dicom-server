// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class IndexableDicomTagServiceTests
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IIndexableDicomTagService _indexableDicomTagService;
        private readonly FeatureConfiguration _featureConfiguration;

        public IndexableDicomTagServiceTests()
        {
            _customTagStore = Substitute.For<ICustomTagStore>();
            _featureConfiguration = new FeatureConfiguration() { EnableCustomQueryTags = true };
            _indexableDicomTagService = new IndexableDicomTagService(_customTagStore, Options.Create(_featureConfiguration));
        }

        [Fact]
        public async Task GivenValidInput_WhenGetCustomTagsIsCalledMultipleTimes_ThenCustomTagStoreIsCalledOnce()
        {
            _customTagStore.GetCustomTagsAsync(null, Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<CustomTagStoreEntry>());

            await _indexableDicomTagService.GetIndexableDicomTagsAsync();
            await _indexableDicomTagService.GetIndexableDicomTagsAsync();
            await _customTagStore.Received(1).GetCustomTagsAsync(null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenEnableCustomQueryTagsIsDisabled_WhenGetCustomTagsIsCalledMultipleTimes_ThenCustomTagStoreShouldNotBeCalled()
        {
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableCustomQueryTags = false };
            IIndexableDicomTagService indexableDicomTagService = new IndexableDicomTagService(_customTagStore, Options.Create(featureConfiguration));
            await indexableDicomTagService.GetIndexableDicomTagsAsync();
            await _customTagStore.DidNotReceive().GetCustomTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
