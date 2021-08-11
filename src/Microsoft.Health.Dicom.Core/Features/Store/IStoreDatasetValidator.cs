// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the requirement.
    /// </summary>
    public interface IDicomDatasetStoreValidator
    {
        /// <summary>
        /// Validates the <paramref name="dicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to validate.</param>
        /// <param name="requiredStudyInstanceUid">
        /// If supplied, the StudyInstanceUID in the <paramref name="dicomDataset"/> must match to be considered valid.
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <exception cref="DatasetValidationException">Thrown when the validation fails.</exception>
        Task ValidateAsync(DicomDataset dicomDataset, string requiredStudyInstanceUid, CancellationToken cancellationToken = default);
    }
}
