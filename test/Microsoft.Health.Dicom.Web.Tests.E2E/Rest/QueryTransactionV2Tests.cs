// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class QueryTransactionV2Tests : QueryTransactionTests
{
    public QueryTransactionV2Tests(HttpIntegrationTestFixture<Startup> fixture) : base(fixture)
    {
    }

    protected override IDicomWebClient GetClient(HttpIntegrationTestFixture<Startup> fixture)
    {
        return fixture.GetDicomWebClient(DicomApiVersions.V2);
    }

    protected override Action<QueryResource, DicomDataset, DicomDataset> GetValidateResponseDataset()
    {
        return ValidationHelpers.ValidateResponseDatasetV2;
    }
}
