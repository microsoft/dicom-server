// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
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

        public static string TokenUrl => GetEnvironmentVariableWithDefault("security_tokenUrl", "https://localhost/connect/token");

        /// <summary>
        /// Set this env in identity provider #73715
        /// </summary>
        public static bool SecurityEnabled
        {
            get
            {
                if (bool.TryParse(GetEnvironmentVariableWithDefault("security_enabled", bool.FalseString), out bool result))
                {
                    return result;
                }

                return false;
            }
        }
    }
}
