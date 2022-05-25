// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Microsoft.Health.Development.IdentityProvider.Configuration;
using Microsoft.Health.Dicom.Tests.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

/// <summary>
/// Gets the Authentication settings to run E2E tests
/// In PR Pipelines: The environment variables are set
/// In Dev Env: It is defaulted to in-prc identity provider settings
/// </summary>
public static class AuthenticationSettings
{
    public static string Scope { get; } = TestEnvironment.Variables["security_scope"] ?? DevelopmentIdentityProviderConfiguration.Audience;

    public static string Resource { get; } = TestEnvironment.Variables["security_resource"] ?? DevelopmentIdentityProviderConfiguration.Audience;

    public static Uri TokenUri { get; } = new Uri(TestEnvironment.Variables["security_tokenUrl"] ?? "https://inprochost/connect/token");

    // This is resolved lazily in case the value is modified
    public static bool SecurityEnabled => string.Equals(TestEnvironment.Variables["security_enabled"] ?? bool.FalseString, bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
}
