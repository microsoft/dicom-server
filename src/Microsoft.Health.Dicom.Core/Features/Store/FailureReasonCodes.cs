// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store;

/// <summary>
/// If any of the failure codes are modified, please check they match the DICOM conformance statement.
/// </summary>
internal static class FailureReasonCodes
{
    /// <summary>
    /// General exception in processing the DICOM instance.
    /// </summary>
    public const ushort ProcessingFailure = 272;

    /// <summary>
    /// Data Set does not contain one or more required attributes.
    /// </summary>
    /// <remarks>
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part07.html#sect_C.5.13"/>
    /// </remarks>
    public const ushort MissingAttribute = 288;

    /// <summary>
    /// Data Set contains one or more attributes which are missing required values.
    /// </summary>
    /// <remarks>
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part07.html#sect_C.5.14"/>
    /// </remarks>
    public const ushort MissingAttributeValue = 289;

    /// <summary>
    /// The DICOM instance failed validation.
    /// </summary>
    public const ushort ValidationFailure = 43264;

    /// <summary>
    /// The DICOM instance already exists.
    /// </summary>
    public const ushort SopInstanceAlreadyExists = 45070;

    /// <summary>
    /// The DICOM instance is being created.
    /// </summary>
    public const ushort PendingSopInstance = 45071;

    /// <summary>
    /// FAILURE - Specified SOP Instance UID does not exist or is not a UPS Instance managed by this SCP
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/dicom/2013/output/chtml/part04/sect_CC.2.html#table_CC.2.2-2"/>
    /// </remarks>
    public const ushort UpsInstanceNotFound = 0xC307;

    /// <summary>
    /// FAILURE - Refused: The UPS may no longer be updated
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/dicom/2013/output/chtml/part04/sect_CC.2.html#table_CC.2.2-2"/>
    /// </remarks>
    public const ushort UpsInstanceUpdateNotAllowed = 0xC300;

    /// <summary>
    /// FAILURE - Refused: The UPS is already COMPLETED
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/dicom/2013/output/chtml/part04/sect_CC.2.html#table_CC.2.2-2"/>
    /// </remarks>
    public const ushort UpsIsAlreadyCompleted = 0xC306;

    /// <summary>
    /// WARNING - The UPS is already in the requested state of CANCELED
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/dicom/2013/output/chtml/part04/sect_CC.2.html#table_CC.2.2-2"/>
    /// </remarks>
    public const ushort UpsIsAlreadyCanceled = 0xC304;

    /// <summary>
    /// FAILURE - Failed: Performer chooses not to cancel
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.2-2"/>
    /// </remarks>
    public const ushort UpsPerformerChoosesNotToCancel = 0xC313;

    /// <summary>
    /// FAILURE - Refused: The UPS is not in the "IN PROGRESS" state
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.6-1"/>
    /// </remarks>
    public const ushort UpsNotInProgressState = 0xC310;

    /// <summary>
    /// FAILURE - Refused: The correct Transaction UID was not provided
    /// </summary>
    /// <remarks>
    /// Hex code are the defined ones in spec.
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#table_CC.2.6-1"/>
    /// </remarks>
    public const ushort UpsTransactionUidIncorrect = 0xC301;

    /// <summary>
    /// FAILURE - Refused: The Transaction UID was not provided
    /// </summary>
    public const ushort UpsTransactionUidAbsent = 0xC302;

    /// <summary>
    /// FAILURE - Procedure step state is present in the dataset provided to be updated which is not allowed.
    /// </summary>
    public const ushort UpsProcedureStepStateNotAllowed = 0xC303;

    /// <summary>
    /// FAILURE - The request is inconsistent with the current state of the Target Workitem. Please try again.
    /// Usually happens when an update request updated the watermark before current request could finish.
    /// </summary>
    public const ushort UpsUpdateConflict = 0xC304;

    /// <summary>
    /// General exception related to blob not found.
    /// </summary>
    public const ushort BlobNotFound = 273;
}
