// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ImagingStudyPropertySynchronizer : IImagingStudyPropertySynchronizer
    {
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly ITableStoreService _tableStoreService;
        private readonly ILogger<ImagingStudyPropertySynchronizer> _logger;

        public ImagingStudyPropertySynchronizer(
            DicomValidationConfiguration dicomValidationConfiguration,
            ITableStoreService tableStoreService,
            ILogger<ImagingStudyPropertySynchronizer> logger)
        {
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(tableStoreService, nameof(tableStoreService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomValidationConfiguration = dicomValidationConfiguration;
            _tableStoreService = tableStoreService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Synchronize(FhirTransactionContext context, ImagingStudy imagingStudy)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(imagingStudy, nameof(imagingStudy));
            EnsureArg.IsNotNull(context.Request.Endpoint, nameof(context.Request.Endpoint));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));

            DicomDataset dataset = context.ChangeFeedEntry.Metadata;

            if (dataset == null)
            {
                return;
            }

            SynchronizePropertiesAsync(imagingStudy, context, false, AddStartedElement);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddImagingStudyEndpoint);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddModality);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddNote);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddAccessionNumber);
        }

        private async void SynchronizePropertiesAsync(ImagingStudy imagingStudy, FhirTransactionContext context, bool required, Action<ImagingStudy, FhirTransactionContext> synchronizeAction, CancellationToken cancellationToken = default)
        {
            try
            {
                synchronizeAction(imagingStudy, context);
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
                    _logger.LogInformation(ex, "Error when synchronizing imaging study data for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID} stored into table storage", studyUID, seriesUID, instanceUID);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void AddNote(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            if (dataset.TryGetSingleValue(DicomTag.StudyDescription, out string description))
            {
                if (!imagingStudy.Note.Any(note => string.Equals(note.Text.Value, description, StringComparison.Ordinal)))
                {
                    Annotation annotation = new Annotation()
                    {
                        Text = new Markdown(description),
                    };

                    imagingStudy.Note.Add(annotation);
                }
            }
        }

        private void AddImagingStudyEndpoint(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            var endpointReference = context.Request.Endpoint.ResourceId.ToResourceReference();

            if (!imagingStudy.Endpoint.Any(endpoint => endpointReference.IsExactly(endpoint)))
            {
                imagingStudy.Endpoint.Add(endpointReference);
            }
        }

        private void AddStartedElement(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            ImagingStudyPipelineHelper.SetDateTimeOffSet(context);
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            TimeSpan utcOffset = context.UtcDateTimeOffset;

            imagingStudy.StartedElement = dataset.GetDateTimePropertyIfNotDefaultValue(DicomTag.StudyDate, DicomTag.StudyTime, utcOffset);
        }

        private void AddModality(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            string modalityInString = ImagingStudyPipelineHelper.GetModalityInString(dataset);

            if (modalityInString != null)
            {
                Coding modality = ImagingStudyPipelineHelper.GetModality(modalityInString);

                List<Coding> existingModalities = imagingStudy.Modality;

                if (dataset.TryGetValues(DicomTag.ModalitiesInStudy, out string[] modalitiesInStudy) &&
                    !existingModalities.Any(existingModality => string.Equals(existingModality.Code, modalityInString, StringComparison.OrdinalIgnoreCase)))
                {
                    imagingStudy.Modality.Add(modality);
                }
            }
        }

        private void AddAccessionNumber(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            string accessionNumber = ImagingStudyPipelineHelper.GetAccessionNumberInString(dataset);
            if (accessionNumber != null)
            {
                Identifier accessionNumberId = ImagingStudyPipelineHelper.GetAccessionNumber(accessionNumber);
                if (!imagingStudy.Identifier.Any(item => accessionNumberId.IsExactly(item)))
                {
                    imagingStudy.Identifier.Add(accessionNumberId);
                }
            }
        }
    }
}
