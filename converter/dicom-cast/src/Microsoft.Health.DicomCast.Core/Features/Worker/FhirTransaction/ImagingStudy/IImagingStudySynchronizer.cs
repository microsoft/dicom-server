// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IImagingStudySynchronizer
    {
        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="imagingStudy"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="imagingStudy">The <see cref="ImagingStudy"/> resource.</param>
        void SynchronizeStudyProperties(FhirTransactionContext context, ImagingStudy imagingStudy);

        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="series"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="series">The <see cref="ImagingStudy.SeriesComponent"/> resource.</param>
        void SynchronizeSeriesProperties(FhirTransactionContext context, ImagingStudy.SeriesComponent series);

        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="instance"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="instance">The <see cref="ImagingStudy.InstanceComponent"/> resource.</param>
        void SynchronizeInstanceProperties(FhirTransactionContext context, ImagingStudy.InstanceComponent instance);
    }
}
