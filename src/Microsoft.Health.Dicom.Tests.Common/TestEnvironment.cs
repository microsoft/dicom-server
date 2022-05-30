// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Tests.Common;

public static class TestEnvironment
{
    // Environment Variables are not case-sensitive on Windows, but they are on Linux/MacOS.
    // In order to normalize this behavior, we load the variables into an IConfiguration which
    // is case-insensitive.
    public static IConfiguration Variables { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();
}
