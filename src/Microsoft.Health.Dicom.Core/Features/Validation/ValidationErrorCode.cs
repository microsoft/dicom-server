// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public enum ValidationErrorCode
    {
        None,
        MultipleElementDetected = 0001,
        ValueExceedMaxLength = 0002,
        ValueNotMatchRequiredLength = 0003,
        ValueContainsInvalidCharacters = 0004,
        InvalidAEExceedMaxLength = 1000,
        InvalidASNotRequiredLength = 1100,
        InvalidAT = 1200,
        InvalidCS = 1300,
        InvalidCSExceedMaxLength = 1301,
        InvalidDA = 1400,
        InvalidDSExceedMaxLength = 1500,
        InvalidDT = 1600,
        InvalidFLNotRequiredLength = 1700,
        InvalidFDNotRequiredLength = 1800,
        InvalidISExceedMaxLength = 1900,
        InvalidLO = 2000,
        InvalidLOTooLong = 2001,
        InvalidLOContainsInvalidCharacters = 2002,
        InvalidLT = 2100,
        InvalidOB = 2200,
        InvalidOD = 2300,
        InvalidOF = 2400,
        InvalidOW = 2500,
        InvalidPN = 2600,
        InvalidPNTooManyGroups = 2601,
        InvalidPNGroupIsTooLong = 2602,
        InvalidPNGroupContainsInvalidCharacters = 2603,
        InvalidPNTooManyComponents = 2604,
        InvalidSH = 2700,
        InvalidSHTooLong = 2701,
        InvalidSLNotRequiredLength = 2800,
        InvalidSQ = 2900,
        InvalidSSNotRequiredLength = 3000,
        InvalidST = 3100,
        InvalidTM = 3200,
        InvalidUI = 3300,
        InvalidULNotRequiredLength = 3400,
        InvalidUN = 3500,
        InvalidUSNotRequiredLength = 3600,
        InvalidUT = 3700,
    }
}
