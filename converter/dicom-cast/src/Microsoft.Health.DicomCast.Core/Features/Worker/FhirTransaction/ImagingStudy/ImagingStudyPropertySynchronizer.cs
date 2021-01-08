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
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudyPropertySynchronizer : IImagingStudyPropertySynchronizer
    {
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly IExceptionStore _exceptionStore;

        public ImagingStudyPropertySynchronizer(
            DicomValidationConfiguration dicomValidationConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _dicomValidationConfiguration = dicomValidationConfiguration;
            _exceptionStore = exceptionStore;
        }

        /// <inheritdoc/>
        public void Synchronize(FhirTransactionContext context, ImagingStudy imagingStudy, CancellationToken cancellationToken)
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

            SynchronizePropertiesAsync(imagingStudy, context, false, AddStartedElement, cancellationToken);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddImagingStudyEndpoint, cancellationToken);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddModality, cancellationToken);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddNote, cancellationToken);
            SynchronizePropertiesAsync(imagingStudy, context, false, AddAccessionNumber, cancellationToken);
        }

        private void SynchronizePropertiesAsync(ImagingStudy imagingStudy, FhirTransactionContext context, bool required, Action<ImagingStudy, FhirTransactionContext> synchronizeAction, CancellationToken cancellationToken = default)
        {
            try
            {
                synchronizeAction(imagingStudy, context);
            }
            catch (Exception ex)
            {
                if (_dicomValidationConfiguration.PartialValidation && !required)
                {
                    DicomDataset dataset = context.ChangeFeedEntry.Metadata;
                    string studyUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                    string seriesUID = dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                    string instanceUID = dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

                    _exceptionStore.StoreException(
                        studyUID,
                        seriesUID,
                        instanceUID,
                        context.ChangeFeedEntry.Sequence,
                        ex,
                        ErrorType.DicomError,
                        cancellationToken);
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
