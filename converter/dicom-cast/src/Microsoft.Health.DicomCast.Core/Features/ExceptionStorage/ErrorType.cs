// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
{
    /// <summary>
    /// Represents the type of error
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// A transient error
        /// </summary>
        Transient,

        /// <summary>
        /// An intransient error that occurs which is not resolvable and thus fail to store to fhir
        /// </summary>
        IntransientError,

        /// <summary>
        /// An intransient error that occurered due to invalid data for a non required entry in the DICOM change feed entry
        /// </summary>
        DicomValidationError,
    }
}
