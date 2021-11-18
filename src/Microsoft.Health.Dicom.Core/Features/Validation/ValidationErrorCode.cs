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
    /// <remarks>
    /// Error Code is smallint/short ranging between [0, 32,767].
    /// For convenience, codes are grouped together by VR where the first 1000 values are agnostic of VR.
    /// By convention, each VR-specific error code should start with the VR name.
    /// e.g. <see cref="PersonNameExceedMaxGroups"/> starts with PersonName.
    /// </remarks>
    [SuppressMessage(category: "Design", checkId: "CA1028: Enum Storage should be Int32", Justification = "Value is stored in SQL as SMALLINT")]
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
        MultiValues = 1,

        /// <summary>
        /// The length of dicom element value exceed max allowed.
        /// </summary>
        ExceedMaxLength = 2,

        /// <summary>
        /// The length of dicom element value is not expected.
        /// </summary>
        UnexpectedLength = 3,

        /// <summary>
        /// The dicom element value has invalid characters.
        /// </summary>
        InvalidCharacters = 4,

        /// <summary>
        /// The VR of dicom element is not expected.
        /// </summary>
        UnexpectedVR = 5,

        /// <summary>
        /// Implicit VR Transfer Syntax is not allowed.
        /// </summary>
        ImplicitVRNotAllowed = 0011,

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

        // Uid specific errors

        /// <summary>
        /// Uid element has invalid value.
        /// </summary>
        UidIsInvalid = 1200,

        // Date Time specific errors

        /// <summary>
        /// DateTime element has invalid value.
        /// </summary>
        DateTimeIsInvalid = 1300,

        // Time specific errors

        /// <summary>
        /// Time element has invalid value.
        /// </summary>
        TimeIsInvalid = 1400,
    }
}
