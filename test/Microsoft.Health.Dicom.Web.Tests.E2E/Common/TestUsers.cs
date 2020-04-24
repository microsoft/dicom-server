// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    public static class TestUsers
    {
        public static TestUser User1 { get; } = new TestUser("user1");

        public static TestUser User2 { get; } = new TestUser("user2");
    }
}
