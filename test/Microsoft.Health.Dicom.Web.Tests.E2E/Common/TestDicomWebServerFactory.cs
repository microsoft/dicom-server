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
    public static TestDicomWebServer GetTestDicomWebServer(Type startupType, DicomTestServerCategory testServerCategory)
    {
        Uri environmentUrl = GetDicomServerUrl(testServerCategory);

        if (environmentUrl == null)
        {
            var enableDataPartitions = (testServerCategory & DicomTestServerCategory.DataPartition) == DicomTestServerCategory.DataPartition;
            var enableDualWrite = (testServerCategory & DicomTestServerCategory.DualWrite) == DicomTestServerCategory.DualWrite;

            return new InProcTestDicomWebServer(startupType, enableDataPartitions, enableDualWrite);
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

    private static Uri GetDicomServerUrl(DicomTestServerCategory testServerCategory)
    {
        var options = new TestEnvironmentOptions();
        TestEnvironment.Variables.Bind(options);

        if ((testServerCategory & DicomTestServerCategory.DataPartition) == DicomTestServerCategory.DataPartition)
        {
            return options.TestFeaturesEnabledEnvironmentUrl;
        }

        if ((testServerCategory & DicomTestServerCategory.Features) == DicomTestServerCategory.Features)
        {
            return options.TestFeaturesEnabledEnvironmentUrl;
        }

        if ((testServerCategory & DicomTestServerCategory.DualWrite) == DicomTestServerCategory.DualWrite)
        {
            return options.TestFeaturesEnabledEnvironmentUrl;
        }

        if (testServerCategory == DicomTestServerCategory.None)
        {
            return options.TestEnvironmentUrl;
        }

        throw new NotSupportedException($"Dicom TestServer Category {testServerCategory} not supported");
    }

    private sealed class TestEnvironmentOptions
    {
        public Uri TestEnvironmentUrl { get; set; }

        public Uri TestFeaturesEnabledEnvironmentUrl { get; set; }
    }
}
