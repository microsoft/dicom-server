// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for <see cref="IInstanceStore"/> where the DB is empty.
    /// </summary>
    public class EmptyInstanceStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly IStoreFactory<IInstanceStore> _instanceStoreFactory;

        public EmptyInstanceStoreTests(SqlDataStoreTestsFixture fixture)
        {
            _instanceStoreFactory = EnsureArg.IsNotNull(fixture?.InstanceStoreFactory, nameof(fixture.InstanceStoreFactory));
        }

        [Fact]
        public async Task GivenEmptyDB_WhenGettingMaxInstanceWatermark_ThenReturnZero()
        {
            IInstanceStore instanceStore = await _instanceStoreFactory.GetInstanceAsync();
            Assert.Equal(0L, await instanceStore.GetMaxInstanceWatermarkAsync());
        }
    }
}
