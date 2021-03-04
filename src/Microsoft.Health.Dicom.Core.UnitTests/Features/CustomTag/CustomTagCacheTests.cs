// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class CustomTagCacheTests
    {
        private ICustomTagStore _customTagStore;
        private ICustomTagCache _customTagCache;

        public CustomTagCacheTests()
        {
            _customTagStore = Substitute.For<ICustomTagStore>();
            _customTagCache = new CustomTagCache(_customTagStore);
        }

        [Fact]
        public async Task GivenValidInput_WhenGetCustomTagsIsCalledMultipleTimes_ThenCustomTagStoreIsCalledOnce()
        {
            _customTagStore.GetCustomTagsAsync(null, Arg.Any<CancellationToken>())
                  .Returns(Array.Empty<CustomTagEntry>());

            await _customTagCache.GetCustomTagsAsync();
            await _customTagCache.GetCustomTagsAsync();
            await _customTagStore.Received(1).GetCustomTagsAsync(null, Arg.Any<CancellationToken>());
        }
    }
}
