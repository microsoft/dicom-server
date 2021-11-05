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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ObservationPipelineStep : IFhirTransactionPipelineStep
    {
        private readonly IObservationDeleteHandler _observationDeleteHandler;
        private readonly IObservationUpsertHandler _observationUpsertHandler;
        private readonly IOptionsMonitor<DicomCastConfiguration> _dicomOptionsMonitor;

        public ObservationPipelineStep(IObservationDeleteHandler observationDeleteHandler,
            IObservationUpsertHandler observationUpsertHandler,
            IOptionsMonitor<DicomCastConfiguration> dicomCastConfiguration)
        {
            _observationDeleteHandler = EnsureArg.IsNotNull(observationDeleteHandler, nameof(observationDeleteHandler));
            _observationUpsertHandler = EnsureArg.IsNotNull(observationUpsertHandler, nameof(observationUpsertHandler));
            _dicomOptionsMonitor = EnsureArg.IsNotNull(dicomCastConfiguration, nameof(dicomCastConfiguration));
        }

        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (_dicomOptionsMonitor.CurrentValue.Features.GenerateObservations)
            {
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
        }

        public void ProcessResponse(FhirTransactionContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            if (_dicomOptionsMonitor.CurrentValue.Features.GenerateObservations)
            {
                if (context.Response?.Observation == null)
                {
                    return;
                }

                foreach (FhirTransactionResponseEntry observation in context.Response.Observation)
                {
                    HttpStatusCode statusCode = observation.Response.Annotation<HttpStatusCode>();

                    // We are only currently doing POSTs which should result in a 201
                    if (statusCode != HttpStatusCode.Created)
                    {
                        throw new ResourceConflictException();
                    }
                }
            }
        }
    }
}
