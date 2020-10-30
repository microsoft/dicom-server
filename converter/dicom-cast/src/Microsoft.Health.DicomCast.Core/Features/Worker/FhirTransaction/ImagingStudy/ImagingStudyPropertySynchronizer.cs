// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Extensions;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ImagingStudyPropertySynchronizer : IImagingStudyPropertySynchronizer
    {
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

            ImagingStudyPipelineHelper.SetDateTimeOffSet(context);

            AddStartedElement(imagingStudy, dataset, context.UtcDateTimeOffset);
            AddImagingStudyEndpoint(imagingStudy, context);
            AddModality(imagingStudy, dataset);
            AddNote(imagingStudy, dataset);
            AddAccessionNumber(imagingStudy, dataset);
        }

        private static void AddNote(ImagingStudy imagingStudy, DicomDataset dataset)
        {
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

        private void AddStartedElement(ImagingStudy imagingStudy, DicomDataset dataset, TimeSpan utcOffset)
        {
            imagingStudy.StartedElement = dataset.GetDateTimePropertyIfNotDefaultValue(DicomTag.StudyDate, DicomTag.StudyTime, utcOffset);
        }

        private void AddModality(ImagingStudy imagingStudy, DicomDataset dataset)
        {
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

        private void AddAccessionNumber(ImagingStudy imagingStudy, DicomDataset dataset)
        {
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
