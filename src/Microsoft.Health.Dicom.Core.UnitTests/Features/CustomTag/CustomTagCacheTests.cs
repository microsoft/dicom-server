// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public class CustomTagCacheTests
    {
        private static readonly TimeSpan DefaultExpirationInterval = TimeSpan.FromSeconds(30);

        [Fact]
        public async Task GivenUninitializedCustomTagCache_WhenGetCustomTagsIsCalled_ShouldInitialize()
        {
            long expectedVersion = 1;
            List<CustomTagEntry> expectedResult = new List<CustomTagEntry>();
            expectedResult.Add(new CustomTagEntry() { Version = expectedVersion });
            ICustomTagStore customTagStore = CreateCustomTagStore(expectedResult, expectedResult);
            CustomTagListCache customTagCache = new CustomTagListCache(customTagStore, DefaultExpirationInterval);
            CustomTagList customTags = await customTagCache.GetCustomTagsAsync();
            Assert.Equal(expectedResult, customTags.CustomTags);
            Assert.NotNull(customTagCache.LastRefreshTime);
        }

        [Fact]
        public async Task GivenOutdatedCustomTagCache_WhenGetCustomTagsIsCalled_ShouldRefreshCache()
        {
            long expectedVersion1 = 1, expectedVersion2 = 2;
            List<CustomTagEntry> expectedResult1 = new List<CustomTagEntry>() { CustomTagTestHelper.CreateCustomTagEntry(version: expectedVersion1) };
            List<CustomTagEntry> expectedResult2 = new List<CustomTagEntry>() { CustomTagTestHelper.CreateCustomTagEntry(version: expectedVersion2) };
            ICustomTagStore customTagStore = CreateCustomTagStore(expectedResult1, expectedResult2);

            CustomTagListCache customTagCache = new CustomTagListCache(customTagStore, TimeSpan.FromMilliseconds(500));
            CustomTagList customTags1 = await customTagCache.GetCustomTagsAsync();
            DateTimeOffset lastRefreshTime = customTagCache.LastRefreshTime.Value;

            // Wait for 1 seconds for cache become invalid
            await Task.Delay(TimeSpan.FromSeconds(1));

            CustomTagList customTags2 = await customTagCache.GetCustomTagsAsync();
            Assert.Equal(expectedResult2, customTags2.CustomTags);
            Assert.NotEqual(lastRefreshTime, customTagCache.LastRefreshTime);
        }

        [Fact]
        public async Task GivenRefreshedCustomTagCache_WhenGetCustomTagsIsCalled_ShouldNotRefreshCache()
        {
            long expectedVersion1 = 1, expectedVersion2 = 2;
            List<CustomTagEntry> expectedResult1 = new List<CustomTagEntry>() { CustomTagTestHelper.CreateCustomTagEntry(expectedVersion1) };
            List<CustomTagEntry> expectedResult2 = new List<CustomTagEntry>() { CustomTagTestHelper.CreateCustomTagEntry(expectedVersion2) };
            ICustomTagStore customTagStore = CreateCustomTagStore(expectedResult1, expectedResult2);

            CustomTagListCache customTagCache = new CustomTagListCache(customTagStore, DefaultExpirationInterval);
            await customTagCache.GetCustomTagsAsync();
            DateTimeOffset lastRefreshTime = customTagCache.LastRefreshTime.Value;
            await Task.Delay(TimeSpan.FromSeconds(1));

            CustomTagList customTags2 = await customTagCache.GetCustomTagsAsync();
            Assert.Equal(expectedResult1, customTags2.CustomTags);
            Assert.Equal(lastRefreshTime, customTagCache.LastRefreshTime);
        }

        [Fact]
        public async Task GivenRefreshedCustomTagCacheWithEmptyList_WhenGetCustomTagsIsCalled_ShouldNotRefreshCache()
        {
            long expectedVersion2 = 2;
            List<CustomTagEntry> expectedResult1 = new List<CustomTagEntry>();
            List<CustomTagEntry> expectedResult2 = new List<CustomTagEntry>() { CustomTagTestHelper.CreateCustomTagEntry(expectedVersion2) };
            ICustomTagStore customTagStore = CreateCustomTagStore(expectedResult1, expectedResult2);

            CustomTagListCache customTagCache = new CustomTagListCache(customTagStore, DefaultExpirationInterval);
            await customTagCache.GetCustomTagsAsync();
            DateTimeOffset lastRefreshTime = customTagCache.LastRefreshTime.Value;
            await Task.Delay(TimeSpan.FromSeconds(1));
            CustomTagList customTags2 = await customTagCache.GetCustomTagsAsync();
            Assert.Equal(expectedResult1, customTags2.CustomTags);
            Assert.Equal(lastRefreshTime, customTagCache.LastRefreshTime);
        }

        private ICustomTagStore CreateCustomTagStore(List<CustomTagEntry> firstCallReturn, List<CustomTagEntry> secondCallReturn)
        {
            ICustomTagStore customTagStore = Substitute.For<ICustomTagStore>();
            customTagStore.TryRefreshCustomTags(out Arg.Any<CustomTagList>(), default)
              .ReturnsForAnyArgs(true);

            customTagStore.WhenForAnyArgs(store => store.TryRefreshCustomTags(out Arg.Any<CustomTagList>(), default))
                .Do(Callback.First(callInfo => callInfo[0] = new CustomTagList(firstCallReturn))
                .ThenKeepDoing(callInfo => callInfo[0] = new CustomTagList(secondCallReturn))
                .AndAlways(callInfo => { }));
            return customTagStore;
        }
    }
}
