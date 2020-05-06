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
        private readonly DicomSqlDataStoreTestsFixture _dicomSqlDataStoreTestsFixture;

        public ChangeFeedTestsFixture()
        {
            _dicomSqlDataStoreTestsFixture = new DicomSqlDataStoreTestsFixture();
        }

        public IDicomIndexDataStore DicomIndexDataStore => _dicomSqlDataStoreTestsFixture.DicomIndexDataStore;

        public IDicomIndexDataStoreTestHelper DicomIndexDataStoreTestHelper => _dicomSqlDataStoreTestsFixture.TestHelper;

        public async Task InitializeAsync()
        {
            await _dicomSqlDataStoreTestsFixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _dicomSqlDataStoreTestsFixture.DisposeAsync();
        }
    }
}
