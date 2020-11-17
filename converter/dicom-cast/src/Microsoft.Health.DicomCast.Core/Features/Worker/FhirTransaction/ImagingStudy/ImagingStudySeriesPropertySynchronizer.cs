// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Extensions;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudySeriesPropertySynchronizer : IImagingStudySeriesPropertySynchronizer
    {
        /// <inheritdoc/>
        public void Synchronize(FhirTransactionContext context, ImagingStudy.SeriesComponent series)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(series, nameof(series));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            // Add series number
            AddSeriesNumber(series, dataset);

            // Add description to series
            AddDescription(series, dataset);

            // Add modality to series
            AddModalityToSeries(series, dataset);

            // Add startedElement to series
            AddStartedElement(series, dataset, context.UtcDateTimeOffset);

            // Add numberOfInstances to series
            AddNumberOfInstances(series, dataset);
        }

        private void AddSeriesNumber(ImagingStudy.SeriesComponent series, DicomDataset dataset)
        {
            if (dataset.TryGetSingleValue(DicomTag.SeriesNumber, out int seriesNumber))
            {
                series.Number = seriesNumber;
            }
        }

        private void AddDescription(ImagingStudy.SeriesComponent series, DicomDataset dataset)
        {
            if (dataset.TryGetSingleValue(DicomTag.SeriesDescription, out string description))
            {
                series.Description = description;
            }
        }

        // Add startedElement to series
        private void AddStartedElement(ImagingStudy.SeriesComponent series, DicomDataset dataset, TimeSpan utcOffset)
        {
            series.StartedElement = dataset.GetDateTimePropertyIfNotDefaultValue(DicomTag.SeriesDate, DicomTag.SeriesTime, utcOffset);
        }

        // Add modality to series
        private void AddModalityToSeries(ImagingStudy.SeriesComponent series, DicomDataset dataset)
        {
            string modalityInString = ImagingStudyPipelineHelper.GetModalityInString(dataset);

            if (modalityInString != null && !string.Equals(series.Modality?.Code, modalityInString, StringComparison.Ordinal))
            {
                series.Modality = ImagingStudyPipelineHelper.GetModality(modalityInString);
            }
        }

        // Add numberOfInstances to series
        private void AddNumberOfInstances(ImagingStudy.SeriesComponent series, DicomDataset dataset)
        {
            if (dataset.TryGetSingleValue(DicomTag.NumberOfSeriesRelatedInstances, out int numberofInstances))
            {
                series.NumberOfInstances = numberofInstances;
            }
        }
    }
}
