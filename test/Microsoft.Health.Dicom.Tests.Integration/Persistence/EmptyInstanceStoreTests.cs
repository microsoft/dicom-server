// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for <see cref="IInstanceStore"/> where the DB is empty.
    /// </summary>
    public class EmptyInstanceStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly IInstanceStore _instanceStore;

        public EmptyInstanceStoreTests(SqlDataStoreTestsFixture fixture)
        {
            _instanceStore = EnsureArg.IsNotNull(fixture?.InstanceStore, nameof(fixture.InstanceStore));
        }

        [Fact]
        public async Task GivenEmptyDB_WhenGettingMaxInstanceWatermark_ThenReturnZero()
        {
            Assert.Equal(0L, await _instanceStore.GetMaxInstanceWatermarkAsync());
        }
    }
}
