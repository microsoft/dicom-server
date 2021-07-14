// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Functions.Management;

namespace Microsoft.Health.Dicom.Functions.Configuration
{
    internal static class DicomFunctionsConfiguration
    {
        public const string SectionName = "DicomFunctions";

        public const string PurgeFrequencyVariable = "%"
            + AzureFunctionsJobHost.ConfigurationSectionName + ":"
            + SectionName + ":"
            + PurgeHistoryOptions.ConfigurationSectionName + ":"
            + nameof(PurgeHistoryOptions.PurgeFrequency) + "%";
    }
}
