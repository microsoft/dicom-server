// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using EnsureThat;
using FellowOakDicom;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudySeriesPropertySynchronizer : IImagingStudySeriesPropertySynchronizer
    {
        private readonly DicomCastConfiguration _dicomCastConfiguration;
        private readonly IExceptionStore _exceptionStore;
        private readonly IEnumerable<(Action<ImagingStudy.SeriesComponent, FhirTransactionContext> PropertyAction, bool RequiredProperty)> _propertiesToSync = new List<(Action<ImagingStudy.SeriesComponent, FhirTransactionContext>, bool)>()
            {
                (AddSeriesNumber, false),
                (AddDescription, false),
                (AddModalityToSeries, true),
                (AddStartedElement, false),
            };

        public ImagingStudySeriesPropertySynchronizer(
            IOptions<DicomCastConfiguration> dicomCastConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(dicomCastConfiguration, nameof(dicomCastConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _dicomCastConfiguration = dicomCastConfiguration.Value;
            _exceptionStore = exceptionStore;
        }

        /// <inheritdoc/>
        public async Task SynchronizeAsync(FhirTransactionContext context, ImagingStudy.SeriesComponent series, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(series, nameof(series));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            foreach (var property in _propertiesToSync)
            {
                await ImagingStudyPipelineHelper.SynchronizePropertiesAsync(series, context, property.PropertyAction, property.RequiredProperty, _dicomCastConfiguration.Features.EnforceValidationOfTagValues, _exceptionStore, cancellationToken);
            }
        }

        private static void AddSeriesNumber(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.SeriesNumber, out int seriesNumber))
            {
                series.Number = seriesNumber;
            }
        }

        private static void AddDescription(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.SeriesDescription, out string description))
            {
                series.Description = description;
            }
        }

        // Add startedElement to series
        private static void AddStartedElement(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            TimeSpan utcOffset = context.UtcDateTimeOffset;
            series.StartedElement = dataset.GetDateTimePropertyIfNotDefaultValue(DicomTag.SeriesDate, DicomTag.SeriesTime, utcOffset);
        }

        // Add modality to series
        private static void AddModalityToSeries(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            string modalityInString = ImagingStudyPipelineHelper.GetModalityInString(dataset);

            if (modalityInString != null && !string.Equals(series.Modality?.Code, modalityInString, StringComparison.Ordinal))
            {
                series.Modality = ImagingStudyPipelineHelper.GetModality(modalityInString);
            }
        }
    }
}
