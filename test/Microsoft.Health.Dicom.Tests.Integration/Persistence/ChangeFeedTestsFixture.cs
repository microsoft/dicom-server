// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Store;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ChangeFeedTestsFixture : IAsyncLifetime
    {
        private readonly SqlDataStoreTestsFixture _sqlDataStoreTestsFixture;

        public ChangeFeedTestsFixture()
        {
            _sqlDataStoreTestsFixture = new SqlDataStoreTestsFixture();
        }

        public IIndexDataStore DicomIndexDataStore => _sqlDataStoreTestsFixture.IndexDataStore;

        public IIndexDataStoreTestHelper DicomIndexDataStoreTestHelper => _sqlDataStoreTestsFixture.TestHelper;

        public async Task InitializeAsync()
        {
            await _sqlDataStoreTestsFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _sqlDataStoreTestsFixture.DisposeAsync();
        }
    }
}
