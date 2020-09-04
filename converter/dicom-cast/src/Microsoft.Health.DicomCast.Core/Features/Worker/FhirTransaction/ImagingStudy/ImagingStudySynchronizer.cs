// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

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

        public void SynchronizeStudyProperties(FhirTransactionContext context, ImagingStudy imagingStudy)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(imagingStudy, nameof(imagingStudy));

            _imagingStudyPropertySynchronizer.Synchronize(context, imagingStudy);
        }

        public void SynchronizeSeriesProperties(FhirTransactionContext context, ImagingStudy.SeriesComponent series)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(series, nameof(series));

            _imagingStudySeriesPropertySynchronizer.Synchronize(context, series);
        }

        public void SynchronizeInstanceProperties(FhirTransactionContext context, ImagingStudy.InstanceComponent instance)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(instance, nameof(instance));

            _imagingStudyInstancePropertySynchronizer.Synchronize(context, instance);
        }
    }
}
