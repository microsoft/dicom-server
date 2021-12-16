// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Functions
{
    public static class AzureFunctionsConfiguration
    {
        public const string RootSectionName = "AzureWebJobsConfigurationSection";
        public const string JobHostSectionName = "AzureFunctionsJobHost";

        public static IConfigurationSource CreateRoot()
            => new MemoryConfigurationSource
            {
                InitialData = new KeyValuePair<string, string>[] { KeyValuePair.Create(RootSectionName, JobHostSectionName) },
            };
    }
}
