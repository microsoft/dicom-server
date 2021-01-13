// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IImagingStudySynchronizer
    {
        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="imagingStudy"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="imagingStudy">The <see cref="ImagingStudy"/> resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task SynchronizeStudyPropertiesAsync(FhirTransactionContext context, ImagingStudy imagingStudy, CancellationToken cancellationToken);

        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="series"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="series">The <see cref="ImagingStudy.SeriesComponent"/> resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task SynchronizeSeriesPropertiesAsync(FhirTransactionContext context, ImagingStudy.SeriesComponent series, CancellationToken cancellationToken);

        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="instance"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="instance">The <see cref="ImagingStudy.InstanceComponent"/> resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task SynchronizeInstancePropertiesAsync(FhirTransactionContext context, ImagingStudy.InstanceComponent instance, CancellationToken cancellationToken);
    }
}
