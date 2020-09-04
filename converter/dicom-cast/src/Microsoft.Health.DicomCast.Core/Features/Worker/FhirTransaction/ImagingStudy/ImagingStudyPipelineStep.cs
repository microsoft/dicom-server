// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net;
using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Pipeline step for handling <see cref="ImagingStudy"/>.
    /// </summary>
    public class ImagingStudyPipelineStep : IFhirTransactionPipelineStep
    {
        private readonly IImagingStudyUpsertHandler _imagingStudyUpsertHandler;
        private readonly IImagingStudyDeleteHandler _imagingStudyDeleteHandler;

        public ImagingStudyPipelineStep(
           IImagingStudyUpsertHandler imagingStudyUpsertHandler,
           IImagingStudyDeleteHandler imagingStudyDeleteHandler)
        {
            EnsureArg.IsNotNull(imagingStudyUpsertHandler, nameof(imagingStudyUpsertHandler));
            EnsureArg.IsNotNull(imagingStudyDeleteHandler, nameof(imagingStudyDeleteHandler));

            _imagingStudyUpsertHandler = imagingStudyUpsertHandler;
            _imagingStudyDeleteHandler = imagingStudyDeleteHandler;
        }

        /// <inheritdoc/>
        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;

            context.Request.ImagingStudy = changeFeedEntry.Action switch
            {
                ChangeFeedAction.Create => await _imagingStudyUpsertHandler.BuildAsync(context, cancellationToken),
                ChangeFeedAction.Delete => await _imagingStudyDeleteHandler.BuildAsync(context, cancellationToken),
                _ => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCastCoreResource.NotSupportedChangeFeedAction,
                        changeFeedEntry.Action)),
            };
        }

        /// <inheritdoc/>
        public void ProcessResponse(FhirTransactionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            // If the ImagingStudy does not exist, we will use conditional create to create the resource
            // to avoid duplicated resource being created. However, if the resource with the identifier
            // was created externally between the retrieve and create, conditional create will return 200
            // and might not contain the changes so we will need to try again.
            if (context.Request.ImagingStudy?.RequestMode == FhirTransactionRequestMode.Create)
            {
                FhirTransactionResponseEntry imagingStudy = context.Response.ImagingStudy;

                HttpStatusCode statusCode = imagingStudy.Response.Annotation<HttpStatusCode>();

                if (statusCode == HttpStatusCode.OK)
                {
                    throw new ResourceConflictException();
                }
            }
        }
    }
}
