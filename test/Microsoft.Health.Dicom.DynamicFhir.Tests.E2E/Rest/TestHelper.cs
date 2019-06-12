// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Rest
{
    internal static class TestHelper
    {
        internal static void AssertSecurityHeaders(HttpResponseHeaders headers)
        {
            Assert.True(headers.TryGetValues("X-Content-Type-Options", out IEnumerable<string> headerValue));
            Assert.Contains("nosniff", headerValue);
        }
    }
}
