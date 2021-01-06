// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.TableStorage;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudySeriesPropertySynchronizer : IImagingStudySeriesPropertySynchronizer
    {
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly ITableStoreService _tableStoreService;
        private readonly ILogger<ImagingStudySeriesPropertySynchronizer> _logger;

        public ImagingStudySeriesPropertySynchronizer(
            DicomValidationConfiguration dicomValidationConfiguration,
            ITableStoreService tableStoreService,
            ILogger<ImagingStudySeriesPropertySynchronizer> logger)
        {
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(tableStoreService, nameof(tableStoreService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomValidationConfiguration = dicomValidationConfiguration;
            _tableStoreService = tableStoreService;
            _logger = logger;
        }

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

            SynchronizePropertiesAsync(series, context, false, AddSeriesNumber);
            SynchronizePropertiesAsync(series, context, false, AddDescription);
            SynchronizePropertiesAsync(series, context, true, AddModalityToSeries);
            SynchronizePropertiesAsync(series, context, false, AddStartedElement);
        }

        private async void SynchronizePropertiesAsync(ImagingStudy.SeriesComponent series, FhirTransactionContext context, bool required, Action<ImagingStudy.SeriesComponent, FhirTransactionContext> synchronizeAction, CancellationToken cancellationToken = default)
        {
            try
            {
                synchronizeAction(series, context);
            }
            catch (Exception ex)
            {
                if (_dicomValidationConfiguration.PartialValidation && _tableStoreService is TableStoreService && !required)
                {
                    DicomDataset dataset = context.ChangeFeedEntry.Metadata;
                    string studyUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                    string seriesUID = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                    string instanceUID = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                    await _tableStoreService.StoreException(
                        studyUID,
                        seriesUID,
                        instanceUID,
                        ex,
                        TableErrorType.DicomError,
                        cancellationToken);
                    _logger.LogInformation(ex, "Error when synchronizing imaging study series data for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID} stored into table storage", studyUID, seriesUID, instanceUID);
                }
                else
                {
                    throw;
                }
            }
        }

        private void AddSeriesNumber(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.SeriesNumber, out int seriesNumber))
            {
                series.Number = seriesNumber;
            }
        }

        private void AddDescription(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.SeriesDescription, out string description))
            {
                series.Description = description;
            }
        }

        // Add startedElement to series
        private void AddStartedElement(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            TimeSpan utcOffset = context.UtcDateTimeOffset;
            series.StartedElement = dataset.GetDateTimePropertyIfNotDefaultValue(DicomTag.SeriesDate, DicomTag.SeriesTime, utcOffset);
        }

        // Add modality to series
        private void AddModalityToSeries(ImagingStudy.SeriesComponent series, FhirTransactionContext context)
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
