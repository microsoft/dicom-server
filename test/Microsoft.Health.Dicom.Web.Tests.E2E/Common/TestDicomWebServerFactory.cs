// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Tests.Common;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

public class TestDicomWebServerFactory
{
    public static TestDicomWebServer GetTestDicomWebServer(Type startupType, bool enableDataPartitions = false)
    {
        var options = new TestEnvironmentOptions();
        TestEnvironment.Variables.Bind(options);

        Uri environmentUrl = enableDataPartitions ? options.TestFeaturesEnabledEnvironmentUrl : options.TestEnvironmentUrl;

        if (environmentUrl == null)
        {
            return new InProcTestDicomWebServer(startupType, enableDataPartitions);
        }
        else if (!environmentUrl.IsAbsoluteUri)
        {
            throw new InvalidOperationException("Environment URL must be absolute");
        }

        if (environmentUrl.AbsoluteUri[^1] != '/')
        {
            environmentUrl = new Uri(environmentUrl.AbsoluteUri + "/", UriKind.Absolute);
        }

        return new RemoteTestDicomWebServer(environmentUrl);
    }

    private sealed class TestEnvironmentOptions
    {
        public Uri TestEnvironmentUrl { get; set; }

        public Uri TestFeaturesEnabledEnvironmentUrl { get; set; }
    }
}
