// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    public class TestDicomWebServerFactory
    {
        public static TestDicomWebServer GetTestDicomWebServer(Type startupType, bool enableDataPartitions = default)
        {
            string environmentUrl = GetEnvironmentUrl(enableDataPartitions);

            if (string.IsNullOrEmpty(environmentUrl))
            {
                return new InProcTestDicomWebServer(startupType, enableDataPartitions);
            }

            if (environmentUrl[^1] != '/')
            {
                environmentUrl += "/";
            }

            return new RemoteTestDicomWebServer(new Uri(environmentUrl));
        }

        private static string GetEnvironmentUrl(bool enableDataPartitions = default)
        {
            return enableDataPartitions ? Environment.GetEnvironmentVariable("TestDataPartitionsEnvironmentUrl") : Environment.GetEnvironmentVariable("TestEnvironmentUrl");
        }
    }
}
