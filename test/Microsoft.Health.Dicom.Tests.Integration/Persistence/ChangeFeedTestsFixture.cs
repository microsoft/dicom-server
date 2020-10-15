// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class ChangeFeedTestsFixture : IDisposable
    {
        private readonly SqlDataStoreTestsFixture _sqlDataStoreTestsFixture;

        public ChangeFeedTestsFixture()
        {
            _sqlDataStoreTestsFixture = new SqlDataStoreTestsFixture();
        }

        public IIndexDataStore DicomIndexDataStore => _sqlDataStoreTestsFixture.IndexDataStore;

        public IIndexDataStoreTestHelper DicomIndexDataStoreTestHelper => _sqlDataStoreTestsFixture.TestHelper;

        public void Initialize()
        {
            _sqlDataStoreTestsFixture.Initialize();
        }

        public void Dispose()
        {
            _sqlDataStoreTestsFixture.Dispose();
        }
    }
}
