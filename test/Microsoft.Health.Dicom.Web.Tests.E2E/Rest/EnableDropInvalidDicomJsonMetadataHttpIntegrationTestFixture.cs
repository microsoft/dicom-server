// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture<TStartup> : HttpIntegrationTestFixture<TStartup>
{
    // because these these rely also on feature enabled e2e tests, we have to have all features enabled here that
    // are enabled there as there is currently no way to isolate the tests
    public EnableDropInvalidDicomJsonMetadataHttpIntegrationTestFixture()
        : base(new[]
        {
            Common.TestServerFeatureSettingType.EnableLatestApiVersion,
            Common.TestServerFeatureSettingType.DataPartition
        })
    { }
}
