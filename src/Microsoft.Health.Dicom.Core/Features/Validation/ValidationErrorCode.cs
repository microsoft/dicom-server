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

        /// <summary>
        /// Dicom element has multiple values -- we only support indexing single value dicom element.
        /// </summary>
        ElementHasMultipleValues = 0001,

        /// <summary>
        ///  Value is too long.
        /// </summary>
        ValueIsTooLong = 0002,

        /// <summary>
        /// Value is not required length.
        /// </summary>
        ValueIsNotRequiredLength = 0003,

        /// <summary>
        /// Value contains invalid characters.
        /// </summary>
        ValueContainsInvalidCharacters = 0004,

        /// <summary>
        /// Patient name has too many groups.
        /// </summary>
        PatientNameHasTooManyGroups = 1000,

        /// <summary>
        /// Group of patient name is too long.
        /// </summary>
        PatientNameGroupIsTooLong = 1001,

        /// <summary>
        ///  Group of patient name contains invalid characters.
        /// </summary>
        PatientNameGroupContainsInvalidCharacters = 1002,

        /// <summary>
        /// Patient name has too many components
        /// </summary>
        PatientNameHasTooManyComponents = 1003,

        /// <summary>
        /// Date is invalid.
        /// </summary>
        InvalidDate = 1100,
    }
}
