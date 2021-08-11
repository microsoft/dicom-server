// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validation on Dicom Dataset.
    /// </summary>
    internal interface IDatasetValidation
    {
        /// <summary>
        /// Validate dicom dataset.
        /// </summary>
        /// <param name="dataset">The dataset.</param>
        void Validate(DicomDataset dataset);
    }
}
