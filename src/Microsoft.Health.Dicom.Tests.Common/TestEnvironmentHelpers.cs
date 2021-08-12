// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class TestEnvironmentHelpers
    {
        public static bool IsTestEnvionrment()
        {
            return !string.IsNullOrEmpty(GetTestEnvironmentUrl());
        }

        public static string GetTestEnvironmentUrl()
        {
            return Environment.GetEnvironmentVariable("TestEnvironmentUrl");
        }
    }
}
