// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.Exceptions
{
    public enum DicomAnonymizationErrorCode
    {
        ParsingJsonConfigurationFailed = 1001,
        MissingConfigurationFields = 1002,
        InvalidConfigurationValues = 1003,
        UnsupportedAnonymizationRule = 1004,
        MissingRuleSettings = 1005,
        InvalidRuleSettings = 1006,

        UnsupportedAnonymizationMethod = 1101,
    }
}
