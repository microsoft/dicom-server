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
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudyPropertySynchronizer : IImagingStudyPropertySynchronizer
    {
        private readonly DicomValidationConfiguration _dicomValidationConfiguration;
        private readonly IExceptionStore _exceptionStore;
        private IEnumerable<(Action<ImagingStudy, FhirTransactionContext> PropertyAction, bool RequiredProperty)> propertiesToSync = new List<(Action<ImagingStudy, FhirTransactionContext>, bool)>()
            {
                (AddStartedElement, false),
                (AddImagingStudyEndpoint, false),
                (AddModality, false),
                (AddNote, false),
                (AddAccessionNumber, false),
            };

        public ImagingStudyPropertySynchronizer(
            IOptions<DicomValidationConfiguration> dicomValidationConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(dicomValidationConfiguration, nameof(dicomValidationConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _dicomValidationConfiguration = dicomValidationConfiguration.Value;
            _exceptionStore = exceptionStore;
        }

        /// <inheritdoc/>
        public async Task SynchronizeAsync(FhirTransactionContext context, ImagingStudy imagingStudy, CancellationToken cancellationToken)
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

            foreach (var property in propertiesToSync)
            {
                await ImagingStudyPipelineHelper.SynchronizePropertiesAsync(imagingStudy, context, property.PropertyAction, property.RequiredProperty, _dicomValidationConfiguration.PartialValidation, _exceptionStore, cancellationToken);
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

        private static void AddImagingStudyEndpoint(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            var endpointReference = context.Request.Endpoint.ResourceId.ToResourceReference();

            if (!imagingStudy.Endpoint.Any(endpoint => endpointReference.IsExactly(endpoint)))
            {
                imagingStudy.Endpoint.Add(endpointReference);
            }
        }

        private static void AddStartedElement(ImagingStudy imagingStudy, FhirTransactionContext context)
        {
            ImagingStudyPipelineHelper.SetDateTimeOffSet(context);
            DicomDataset dataset = context.ChangeFeedEntry.Metadata;
            TimeSpan utcOffset = context.UtcDateTimeOffset;

            imagingStudy.StartedElement = dataset.GetDateTimePropertyIfNotDefaultValue(DicomTag.StudyDate, DicomTag.StudyTime, utcOffset);
        }

        private static void AddModality(ImagingStudy imagingStudy, FhirTransactionContext context)
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

        private static void AddAccessionNumber(ImagingStudy imagingStudy, FhirTransactionContext context)
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
