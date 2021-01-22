// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudySynchronizer : IImagingStudySynchronizer
    {
        private readonly IImagingStudyPropertySynchronizer _imagingStudyPropertySynchronizer;
        private readonly IImagingStudySeriesPropertySynchronizer _imagingStudySeriesPropertySynchronizer;
        private readonly IImagingStudyInstancePropertySynchronizer _imagingStudyInstancePropertySynchronizer;

        public ImagingStudySynchronizer(
           IImagingStudyPropertySynchronizer imagingStudyPropertySynchronizer,
           IImagingStudySeriesPropertySynchronizer imagingStudySeriesPropertySynchronizer,
           IImagingStudyInstancePropertySynchronizer imagingStudyInstancePropertySynchronizer)
        {
            EnsureArg.IsNotNull(imagingStudyPropertySynchronizer, nameof(imagingStudyPropertySynchronizer));
            EnsureArg.IsNotNull(imagingStudySeriesPropertySynchronizer, nameof(imagingStudySeriesPropertySynchronizer));
            EnsureArg.IsNotNull(imagingStudyInstancePropertySynchronizer, nameof(imagingStudyInstancePropertySynchronizer));

            _imagingStudyPropertySynchronizer = imagingStudyPropertySynchronizer;
            _imagingStudySeriesPropertySynchronizer = imagingStudySeriesPropertySynchronizer;
            _imagingStudyInstancePropertySynchronizer = imagingStudyInstancePropertySynchronizer;
        }

        public async Task SynchronizeStudyPropertiesAsync(FhirTransactionContext context, ImagingStudy imagingStudy, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(imagingStudy, nameof(imagingStudy));

            await _imagingStudyPropertySynchronizer.SynchronizeAsync(context, imagingStudy, cancellationToken);
        }

        public async Task SynchronizeSeriesPropertiesAsync(FhirTransactionContext context, ImagingStudy.SeriesComponent series, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(series, nameof(series));

            await _imagingStudySeriesPropertySynchronizer.SynchronizeAsync(context, series, cancellationToken);
        }

        public async Task SynchronizeInstancePropertiesAsync(FhirTransactionContext context, ImagingStudy.InstanceComponent instance, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(instance, nameof(instance));

            await _imagingStudyInstancePropertySynchronizer.SynchronizeAsync(context, instance, cancellationToken);
        }
    }
}
