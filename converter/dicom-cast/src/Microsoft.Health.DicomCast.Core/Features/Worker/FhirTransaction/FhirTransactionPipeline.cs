// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to build and process response of a FHIR transaction.
    /// </summary>
    public class FhirTransactionPipeline : IFhirTransactionPipeline
    {
        private readonly IEnumerable<IFhirTransactionPipelineStep> _fhirTransactionPipelines;
        private readonly IFhirTransactionExecutor _fhirTransactionExecutor;

        private readonly IReadOnlyList<FhirTransactionRequestResponsePropertyAccessor> _requestResponsePropertyAccessors;

        public FhirTransactionPipeline(
            IEnumerable<IFhirTransactionPipelineStep> fhirTransactionPipelines,
            IFhirTransactionRequestResponsePropertyAccessors fhirTransactionRequestResponsePropertyAccessors,
            IFhirTransactionExecutor fhirTransactionExecutor)
        {
            EnsureArg.IsNotNull(fhirTransactionPipelines, nameof(fhirTransactionPipelines));
            EnsureArg.IsNotNull(fhirTransactionRequestResponsePropertyAccessors, nameof(fhirTransactionRequestResponsePropertyAccessors));
            EnsureArg.IsNotNull(fhirTransactionExecutor, nameof(fhirTransactionExecutor));

            _fhirTransactionPipelines = fhirTransactionPipelines;
            _fhirTransactionExecutor = fhirTransactionExecutor;

            _requestResponsePropertyAccessors = fhirTransactionRequestResponsePropertyAccessors.PropertyAccessors;
        }

        /// <inheritdoc/>
        public async Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(changeFeedEntry, nameof(changeFeedEntry));

            // Create a context used throughout this process.
            var context = new FhirTransactionContext(changeFeedEntry);

            // Prepare all required objects for the transaction.
            foreach (IFhirTransactionPipelineStep pipeline in _fhirTransactionPipelines)
            {
                await pipeline.PrepareRequestAsync(context, cancellationToken);
            }

            // Check to see if any resource needs to be created/updated.
            var bundle = new Bundle()
            {
                Type = Bundle.BundleType.Transaction,
            };

            var usedPropertyAccessors = new List<FhirTransactionRequestResponsePropertyAccessor>(_requestResponsePropertyAccessors.Count);

            foreach (FhirTransactionRequestResponsePropertyAccessor propertyAccessor in _requestResponsePropertyAccessors)
            {
                FhirTransactionRequestEntry requestEntry = propertyAccessor.RequestEntryGetter(context.Request);

                if (requestEntry == null || requestEntry.RequestMode == FhirTransactionRequestMode.None)
                {
                    // No associated request, skip it.
                    continue;
                }

                // There is a associated request, add to the list so it gets processed.
                usedPropertyAccessors.Add(propertyAccessor);
                bundle.Entry.Add(CreateRequestBundleEntryComponent(requestEntry));
            }

            if (!bundle.Entry.Any())
            {
                // Nothing to update.
                return;
            }

            // Execute the transaction.
            Bundle responseBundle = await _fhirTransactionExecutor.ExecuteTransactionAsync(bundle, cancellationToken);

            // Process the response.
            for (int i = 0; i < usedPropertyAccessors.Count; i++)
            {
                FhirTransactionResponseEntry responseEntry = CreateResponseEntry(responseBundle.Entry[i]);

                usedPropertyAccessors[i].ResponseEntrySetter(context.Response, responseEntry);
            }

            // Execute any additional checks of the response.
            foreach (IFhirTransactionPipelineStep pipeline in _fhirTransactionPipelines)
            {
                pipeline.ProcessResponse(context);
            }

            static Bundle.EntryComponent CreateRequestBundleEntryComponent(FhirTransactionRequestEntry requestEntry)
            {
                return new Bundle.EntryComponent()
                {
                    FullUrl = requestEntry.ResourceId.ToString(),
                    Request = requestEntry.Request,
                    Resource = requestEntry.Resource,
                };
            }

            static FhirTransactionResponseEntry CreateResponseEntry(Bundle.EntryComponent response)
            {
                return new FhirTransactionResponseEntry(response.Response, response.Resource);
            }
        }
    }
}
