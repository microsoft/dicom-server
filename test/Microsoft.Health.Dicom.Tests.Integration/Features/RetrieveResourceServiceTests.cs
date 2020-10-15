// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Dicom.Tests.Integration.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class RetrieveResourceServiceTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public RetrieveResourceServiceTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Penche_CustomTest()
        {
            await using (SqlDataStoreTestsFixture sql = new SqlDataStoreTestsFixture())
            {
                sql.OutputHelper = _testOutputHelper;
                await sql.InitializeAsync();
            }
        }
    }
}
