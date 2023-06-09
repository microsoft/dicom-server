// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class QueryTransactionLatestTests : QueryTransactionTests
{
    public QueryTransactionLatestTests(HttpIntegrationTestFixture<Startup> fixture) : base(fixture)
    {
    }

    protected override IDicomWebClient GetClient(HttpIntegrationTestFixture<Startup> fixture)
    {
        return fixture.GetDicomWebClient(DicomApiVersions.Latest);
    }

    protected override void ValidateResponseDataset(QueryResource resource, DicomDataset expected, DicomDataset actual)
    {
        ValidationHelpers.ValidateResponseDatasetV2(resource, expected, actual);
    }
}
