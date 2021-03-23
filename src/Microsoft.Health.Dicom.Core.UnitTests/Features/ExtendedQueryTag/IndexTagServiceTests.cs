// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class IndexTagServiceTests
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexTagService _indexTagService;
        private readonly FeatureConfiguration _featureConfiguration;

        public IndexTagServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = true };
            _indexTagService = new IndexTagService(_extendedQueryTagStore, Options.Create(_featureConfiguration));
        }

        [Fact]
        public async Task GivenValidInput_WhenGetExtendedQueryTagsIsCalledMultipleTimes_ThenExtendedQueryTagStoreIsCalledOnce()
        {
            _extendedQueryTagStore.GetExtendedQueryTagsAsync(null, Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<ExtendedQueryTagStoreEntry>());

            await _indexTagService.GetIndexTagsAsync();
            await _indexTagService.GetIndexTagsAsync();
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync(null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenEnableExtendedQueryTagsIsDisabled_WhenGetExtendedQueryTagsIsCalledMultipleTimes_ThenExtendedQueryTagStoreShouldNotBeCalled()
        {
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = false };
            IIndexTagService indexableDicomTagService = new IndexTagService(_extendedQueryTagStore, Options.Create(featureConfiguration));
            await indexableDicomTagService.GetIndexTagsAsync();
            await _extendedQueryTagStore.DidNotReceive().GetExtendedQueryTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
