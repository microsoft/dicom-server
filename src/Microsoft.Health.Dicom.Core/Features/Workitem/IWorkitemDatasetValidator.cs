// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Peforms validation on incoming dataset that will be added as a workitem
    /// </summary>
    public interface IWorkitemDatasetValidator
    {
        string Name { get; }

        /// <summary>
        /// Validates the <paramref name="dicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to validate.</param>
        /// <exception cref="DatasetValidationException">Thrown when the validation fails.</exception>
        void Validate(DicomDataset dicomDataset);
    }
}
