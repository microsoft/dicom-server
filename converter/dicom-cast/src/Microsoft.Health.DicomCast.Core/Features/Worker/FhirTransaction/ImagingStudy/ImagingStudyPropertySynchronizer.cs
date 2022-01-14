// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ImagingStudyPropertySynchronizer : IImagingStudyPropertySynchronizer
    {
        private readonly DicomCastConfiguration _dicomCastConfiguration;
        private readonly IExceptionStore _exceptionStore;
        private readonly IEnumerable<(Action<ImagingStudy, FhirTransactionContext> PropertyAction, bool RequiredProperty)> _propertiesToSync = new List<(Action<ImagingStudy, FhirTransactionContext>, bool)>()
            {
                (AddStartedElement, false),
                (AddImagingStudyEndpoint, false),
                (AddModality, false),
                (AddNote, false),
                (AddAccessionNumber, false),
            };

        public ImagingStudyPropertySynchronizer(
            IOptions<DicomCastConfiguration> dicomCastConfiguration,
            IExceptionStore exceptionStore)
        {
            EnsureArg.IsNotNull(dicomCastConfiguration, nameof(dicomCastConfiguration));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            _dicomCastConfiguration = dicomCastConfiguration.Value;
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

            foreach (var property in _propertiesToSync)
            {
                await ImagingStudyPipelineHelper.SynchronizePropertiesAsync(imagingStudy, context, property.PropertyAction, property.RequiredProperty, _dicomCastConfiguration.Features.EnforceValidationOfTagValues, _exceptionStore, cancellationToken);
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
