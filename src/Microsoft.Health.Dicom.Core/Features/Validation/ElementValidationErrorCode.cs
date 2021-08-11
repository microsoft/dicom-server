// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validation Error Code.
    /// Error Code is a short number (from 0 to 32767), and organized in this way:
    /// 0 => No error.
    /// [1,100) => General error for all VR.
    /// [100,200) => Error for PN.
    /// [200,300) => Error for DA.
    /// Each VR could have up to 100 error code.
    /// </summary>
    public enum ElementValidationErrorCode
    {
        /// <summary>
        /// No errors.
        /// </summary>
        None,

        /// <summary>
        /// Dicom element has multiple values -- we only support indexing single value dicom element.
        /// </summary>
        ElementHasMultipleValues = 1,

        /// <summary>
        ///  Value length exceeds max length
        /// </summary>
        ValueLengthExceedsMaxLength = 2,

        /// <summary>
        /// Value length is not required length.
        /// </summary>
        ValueLengthIsNotRequiredLength = 3,

        /// <summary>
        /// Value contains invalid characters.
        /// </summary>
        ValueContainsInvalidCharacters = 4,

        /// <summary>
        /// Element has wrong VR.
        /// </summary>
        ElementHasWrongVR = 5,

        /// <summary>
        /// Patient name has too many groups.
        /// </summary>
        PatientNameHasTooManyGroups = 100,

        /// <summary>
        /// Group of patient name is too long.
        /// </summary>
        PatientNameGroupIsTooLong = 101,

        /// <summary>
        ///  Group of patient name contains invalid characters.
        /// </summary>
        PatientNameGroupContainsInvalidCharacters = 102,

        /// <summary>
        /// Patient name has too many components
        /// </summary>
        PatientNameHasTooManyComponents = 103,

        /// <summary>
        /// Date is invalid.
        /// </summary>
        InvalidDate = 200,
    }
}
