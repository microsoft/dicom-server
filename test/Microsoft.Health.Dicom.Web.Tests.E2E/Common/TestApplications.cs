// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    public static class TestApplications
    {
        public static TestApplication GlobalAdminServicePrincipal { get; } = new TestApplication("globalAdminServicePrincipal");

        public static TestApplication InvalidClient { get; } = new TestApplication("invalidClient");
    }
}
