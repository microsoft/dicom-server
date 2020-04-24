// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.Health.Development.IdentityProvider.Configuration;
using static Microsoft.Health.Dicom.Tests.Common.EnvironmentVariables;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    public static class AuthenticationSettings
    {
        public static string Scope => GetEnvironmentVariableWithDefault("Scope", Provider.Audience);

        public static string Resource => GetEnvironmentVariableWithDefault("Resource", Provider.Audience);

        public static string TokenUrl => GetEnvironmentVariableWithDefault("security_tokenUrl", string.Empty);

        public static bool SecurityEnabled => !string.IsNullOrWhiteSpace(TokenUrl);
    }
}
