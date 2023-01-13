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
    public static TestDicomWebServer GetTestDicomWebServer(Type startupType, TestServerFeatureSettingType[] featureSettingTypes)
    {
        Uri environmentUrl = GetDicomServerUrl();

        if (environmentUrl == null)
        {
            return new InProcTestDicomWebServer(startupType, featureSettingTypes);
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

    private static Uri GetDicomServerUrl()
    {
        var options = new TestEnvironmentOptions();
        TestEnvironment.Variables.Bind(options);
        return options.TestEnvironmentUrl;
    }

    private sealed class TestEnvironmentOptions
    {
        public Uri TestEnvironmentUrl { get; set; }
    }
}
