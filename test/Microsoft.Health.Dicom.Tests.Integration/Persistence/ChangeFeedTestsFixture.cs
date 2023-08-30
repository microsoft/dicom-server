// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class ChangeFeedTestsFixture : SqlDataStoreTestsFixture
{
    public ChangeFeedTestsFixture() : base()
    {
        PreviousDicomChangeFeedStore = new SqlChangeFeedStoreV39(SqlConnectionWrapperFactory);
    }

    public IIndexDataStore DicomIndexDataStore => IndexDataStore;

    public IChangeFeedStore DicomChangeFeedStore => ChangeFeedStore;

    public IChangeFeedStore PreviousDicomChangeFeedStore { get; }

    public IIndexDataStoreTestHelper DicomIndexDataStoreTestHelper => IndexDataStoreTestHelper;
}
