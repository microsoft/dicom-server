// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store
{
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
        /// The DICOM instance does not belong to the specified study.
        /// </summary>
        public const ushort MismatchStudyInstanceUid = 43265;

        /// <summary>
        /// The DICOM instance already exists.
        /// </summary>
        public const ushort SopInstanceAlreadyExists = 45070;

        /// <summary>
        /// The DICOM instance is being created.
        /// </summary>
        public const ushort PendingSopInstance = 45071;

        /// <summary>
        /// Data Set does not match SOP Class
        /// </summary>
        /// <remarks>
        /// The Studies Store Transaction (Section 10.5) observed that the Data Set did not match the constraints of the SOP Class during storage of the instance.
        /// </remarks>
        public const ushort DatasetDoesNotMatchSOPClass = 45063;
    }
}
