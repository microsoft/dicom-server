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
    /// ErrorCode naming convention:
    /// VR specific error code should start with VR name.
    /// e.g: PatientNameGroupIsTooLong starts with PatientName
    /// </summary>
    public enum ValidationErrorCode
    {
        None = 0,

        // General Errors
        MultiValues = 0001,
        ExceedMaxLength = 0002,
        NotRequiredLength = 0003,
        InvalidCharacters = 0004,
        NotRequiredVR = 0005,

        // Patient Name specific errors
        PersonNameExceedMaxGroups = 1000,
        PersonNameGroupExceedMaxLength = 1001,
        PersonNameExceedMaxComponents = 1002,

        // Date specific errors
        DateIsInvalid = 1100,

        // Uid specific errors
        UidIsInvalid = 1200,
    }
}
