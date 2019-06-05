// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public interface IDicomDataStore
    {
        /// <summary>
        /// Stores the provided stream as a DICOM file.
        /// </summary>
        /// <param name="dicomFile">The raw DICOM file to store.</param>
        /// <param name="studyInstanceUID">The expected study instance identifier of the DICOM file stream or null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the store request.</returns>
        Task<bool> StoreDicomFileAsync(DicomFile dicomFile, string studyInstanceUID = null, CancellationToken cancellationToken = default);
    }
}
