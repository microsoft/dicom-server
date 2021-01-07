// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
{
    /// <summary>
    /// Represents the type of error that is going to be stored
    /// </summary>
    public enum TableErrorType
    {
        /// <summary>
        /// A transient error
        /// </summary>
        Transient,

        /// <summary>
        /// An intransient error that occured from the fhir service
        /// </summary>
        FhirError,

        /// <summary>
        /// An intransient error that occurered due to invalid data in the DICOM change feed entry
        /// </summary>
        DicomError,
    }
}
