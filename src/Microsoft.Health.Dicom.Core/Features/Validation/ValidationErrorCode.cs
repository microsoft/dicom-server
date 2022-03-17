// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// Represents a problem with the value for a given DICOM Value Representation (VR).
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
    #region General

    /// <summary>
    /// No error
    /// </summary>
    None = 0,

    /// <summary>
    /// The dicom element has multiple values.
    /// </summary>
    MultipleValues = 1,

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
    ImplicitVRNotAllowed = 6,

    #endregion

    #region Person Name (PN)

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

    #endregion

    #region Date (DA)

    /// <summary>
    /// Date element has invalid value.
    /// </summary>
    DateIsInvalid = 1100,

    #endregion

    #region Unique Identifier (UI)

    /// <summary>
    /// Uid element has invalid value.
    /// </summary>
    UidIsInvalid = 1200,

    #endregion

    #region Date Time (DT)

    /// <summary>
    /// Date Time element has invalid value.
    /// </summary>
    DateTimeIsInvalid = 1300,

    #endregion

    #region Time (TM)

    /// <summary>
    /// Time element has invalid value.
    /// </summary>
    TimeIsInvalid = 1400,

    #endregion

    #region Integer String (IS)

    /// <summary>
    /// Integer String element has an invalid value.
    /// </summary>
    IntegerStringIsInvalid = 1500,

    #endregion

    #region Sequence of Items (SQ)

    /// <summary>
    /// Sequences are not allowed.
    /// </summary>
    SequenceDisallowed = 1600,

    /// <summary>
    /// Nested sequences are not allowed.
    /// </summary>
    NestedSequence = 1601,

    #endregion
}
