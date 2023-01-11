// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture<TStartup> : HttpIntegrationTestFixture<TStartup>
{
    public EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture()
        : base(Common.TestServerFeatureSettingType.EnableDropInvalidDicomJsonMetadata)
    { }
}
