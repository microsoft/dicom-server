// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Anonymizer.Common.Settings
{
    public static class DateTimeGlobalSettings
    {
        public static string DateFormat { get; set; } = "yyyy-MM-dd";

        public static string DateTimeFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss";

        public static string YearFormat { get; set; } = "yyyy";

        // Refer to HIPPA standard https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html
        public static int AgeThreshold { get; set; } = 89;
    }
}
