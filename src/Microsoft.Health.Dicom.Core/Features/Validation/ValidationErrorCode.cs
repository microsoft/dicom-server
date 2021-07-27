// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validation Error Code.
    /// Error Code is  4 letter number. 
    /// First 2 letters indicate VR:
    ///  00 - General error for all VR
    ///  01 - Error for PN
    /// Last 2 letters indicate specific errors for VR.
    /// </summary>
    public enum ValidationErrorCode
    {
        None = 0,

        // General Errors
        ValueHasMultipleItems = 0001,
        ValueExceedMaxLength = 0002,
        ValueNotMatchRequiredLength = 0003,
        ValueContainsInvalidCharacters = 0004,

        // Patient Name specific errors
        PatientNameHasTooManyGroups = 1000,
        PatientNameGroupIsTooLong = 1001,
        PatientNameGroupContainsInvalidCharacters = 1002,
        PatientNameHasTooManyComponents = 1003,

        // Date specific errors
        InvalidDate = 1100,
    }
}
