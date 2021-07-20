// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Utility;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ObservationPipelineStep : IFhirTransactionPipelineStep
    {
        private readonly IObservationUpsertHandler _observationUpsertHandler;
        private readonly IObservationDeleteHandler _observationDeleteHandler;

        public ObservationPipelineStep(
            IObservationUpsertHandler observationUpsertHandler,
            IObservationDeleteHandler observationDeleteHandler)
        {
            EnsureArg.IsNotNull(observationUpsertHandler, nameof(observationUpsertHandler));
            EnsureArg.IsNotNull(observationDeleteHandler, nameof(observationDeleteHandler));

            _observationUpsertHandler = observationUpsertHandler;
            _observationDeleteHandler = observationDeleteHandler;
        }

        /// <inheritdoc/>
        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;
            context.Request.Observation = changeFeedEntry.Action switch
            {
                ChangeFeedAction.Create => await _observationUpsertHandler.BuildAsync(context, cancellationToken),
                ChangeFeedAction.Delete => await _observationDeleteHandler.BuildAsync(context, cancellationToken),
                _ => throw new NotSupportedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCastCoreResource.NotSupportedChangeFeedAction,
                        changeFeedEntry.Action))
            };
        }

        /// <inheritdoc/>
        public void ProcessResponse(FhirTransactionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            if (context.Request?.Observation?.RequestMode != FhirTransactionRequestMode.Create)
            {
                return;
            }

            HttpStatusCode statusCode = context.Response.Observation.Response.Annotation<HttpStatusCode>();
            if (statusCode == HttpStatusCode.OK)
            {
                throw new ResourceConflictException();
            }
        }
    }
}
