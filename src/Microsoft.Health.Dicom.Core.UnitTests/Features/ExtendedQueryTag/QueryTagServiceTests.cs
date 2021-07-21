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
    public class QueryTagServiceTests
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IQueryTagService _queryTagService;
        private readonly FeatureConfiguration _featureConfiguration;

        public QueryTagServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = true };
            _queryTagService = new QueryTagService(_extendedQueryTagStore, Options.Create(_featureConfiguration));
        }

        [Fact]
        public async Task GivenValidInput_WhenGetExtendedQueryTagsIsCalledMultipleTimes_ThenExtendedQueryTagStoreIsCalledOnce()
        {
            _extendedQueryTagStore.GetExtendedQueryTagsAsync((string)null, Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<ExtendedQueryTagStoreEntry>());

            await _queryTagService.GetQueryTagsAsync();
            await _queryTagService.GetQueryTagsAsync();
            await _extendedQueryTagStore.Received(1).GetExtendedQueryTagsAsync((string)null, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenEnableExtendedQueryTagsIsDisabled_WhenGetExtendedQueryTagsIsCalledMultipleTimes_ThenExtendedQueryTagStoreShouldNotBeCalled()
        {
            FeatureConfiguration featureConfiguration = new FeatureConfiguration() { EnableExtendedQueryTags = false };
            IQueryTagService indexableDicomTagService = new QueryTagService(_extendedQueryTagStore, Options.Create(featureConfiguration));
            await indexableDicomTagService.GetQueryTagsAsync();
            await _extendedQueryTagStore.DidNotReceive().GetExtendedQueryTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}
