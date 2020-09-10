// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IImagingStudyPropertySynchronizer
    {
        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="imagingStudy"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="imagingStudy">The <see cref="ImagingStudy"/> resource.</param>
        void Synchronize(FhirTransactionContext context, ImagingStudy imagingStudy);
    }
}
