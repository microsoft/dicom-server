// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class WorkitemTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly SqlDataStoreTestsFixture _fixture;

        public WorkitemTests(SqlDataStoreTestsFixture fixture)
        {
            _fixture = fixture;
        }

        //[Fact]
        //public async Task WhenValidWorkitemIsCreated_CreationSucceeds()
        //{
        //    string workitemUid = "2.25.1234";

        //    long workitemKey = await _fixture.WorkitemStore.AddWorkitemAsync(DefaultPartition.Key, workitemUid, CancellationToken.None);

        //    Assert.True(workitemKey > 0, "WorkitemKey not returned.");
        //}
    }
}
