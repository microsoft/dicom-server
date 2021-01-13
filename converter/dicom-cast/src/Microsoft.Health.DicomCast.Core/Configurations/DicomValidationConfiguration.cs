// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Configurations
{
    /// <summary>
    /// Level of validation for Dicom data
    /// </summary>
    public class DicomValidationConfiguration
    {
        /// <summary>
        /// If partial validation is enabled or not. If false then only change feed entries with completely validated
        /// data will be stored in fhir.
        /// </summary>
        public bool PartialValidation { get; set; }
    }
}
