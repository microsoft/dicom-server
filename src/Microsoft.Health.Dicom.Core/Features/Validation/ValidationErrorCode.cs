// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validation Error Code.
    /// </summary>
    /// <remarks>Error Code is  4 letter number. 
    /// First 2 letters indicate VR:
    ///  00 - General error for all VR
    ///  01 - Error for PN
    /// Last 2 letters indicate specific errors for VR.
    /// ErrorCode naming convention:
    /// VR specific error code should start with VR name.
    /// e.g: PatientNameGroupIsTooLong starts with PatientName.
    /// </remarks>
    [SuppressMessage(category: "Design", checkId: "CA1028: Enum Storage should be Int32", Justification = "aule is stroed in SQL as SMALLINT")]
    public enum ValidationErrorCode : short
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,

        // General Errors

        /// <summary>
        /// The dicom element has multiple values.
        /// </summary>
        MultiValues = 0001,

        /// <summary>
        /// The length of dicom element value exceed max allowed.
        /// </summary>
        ExceedMaxLength = 0002,

        /// <summary>
        /// The length of dicom element value is not expected.
        /// </summary>
        UnexpectedLength = 0003,

        /// <summary>
        /// The dicom element value has invalid characters.
        /// </summary>
        InvalidCharacters = 0004,

        /// <summary>
        /// The VR of dicom element is not expected.
        /// </summary>
        UnexpectedVR = 0005,

        // Person Name specific errors

        /// <summary>
        /// Person name element has more than allowed groups.
        /// </summary>
        PersonNameExceedMaxGroups = 1000,

        /// <summary>
        /// The length of person name group exceed max allowed.
        /// </summary>
        PersonNameGroupExceedMaxLength = 1001,

        /// <summary>
        /// Person name element has more than allowed component.
        /// </summary>
        PersonNameExceedMaxComponents = 1002,

        // Date specific errors

        /// <summary>
        /// Date element has invalid value.
        /// </summary>
        DateIsInvalid = 1100,

        /// <summary>
        /// DateTime element has invalid value.
        /// </summary>
        DateTimeIsInvalid = 1101,

        /// <summary>
        /// Time element has invalid value.
        /// </summary>
        TimeIsInvalid = 1102,

        // Uid specific errors

        /// <summary>
        /// Uid element has invalid value.
        /// </summary>
        UidIsInvalid = 1200,
    }
}
