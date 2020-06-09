// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Microsoft.Health.Development.IdentityProvider.Configuration;
using static Microsoft.Health.Dicom.Tests.Common.EnvironmentVariables;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    /// <summary>
    /// Gets the Authentication settings to run E2E tests
    /// In PR Pipelines: The environment variables are set
    /// In Dev Env: It is defaulted to in-prc identity provider settings
    /// </summary>
    public static class AuthenticationSettings
    {
        public static string Scope => GetEnvironmentVariableWithDefault("security_scope", Provider.Audience);

        public static string Resource => GetEnvironmentVariableWithDefault("security_resource", Provider.Audience);

        public static Uri TokenUri => new Uri(GetEnvironmentVariableWithDefault("security_tokenUrl", "https://inprochost/connect/token"));

        public static bool SecurityEnabled => string.Equals(GetEnvironmentVariableWithDefault("security_enabled", bool.FalseString), bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
    }
}
