// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Tests.Common;

public static class TestEnvironment
{
    public static IConfiguration Variables { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();
}
