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

            var usedPropertyAccessors = new List<(FhirTransactionRequestResponsePropertyAccessor Accessor, int Count)>(_requestResponsePropertyAccessors.Count);

            foreach (FhirTransactionRequestResponsePropertyAccessor propertyAccessor in _requestResponsePropertyAccessors)
            {
                List<FhirTransactionRequestEntry> requestEntries = propertyAccessor.RequestEntryGetter(context.Request)?.ToList();

                if (requestEntries == null || requestEntries.Count == 0)
                {
                    continue;
                }

                int useCount = 0;
                foreach (FhirTransactionRequestEntry requestEntry in requestEntries)
                {
                    if (requestEntry == null || requestEntry.RequestMode == FhirTransactionRequestMode.None)
                    {
                        // No associated request, skip it.
                        continue;
                    }

                    // There is a associated request, add to the list so it gets processed.
                    bundle.Entry.Add(CreateRequestBundleEntryComponent(requestEntry));
                    useCount++;
                }

                usedPropertyAccessors.Add((propertyAccessor, useCount));
            }

            if (bundle.Entry.Count == 0)
            {
                // Nothing to update.
                return;
            }

            // Execute the transaction.
            Bundle responseBundle = await _fhirTransactionExecutor.ExecuteTransactionAsync(bundle, cancellationToken);

            // Process the response.
            int processedResponseItems = 0;

            foreach ((FhirTransactionRequestResponsePropertyAccessor accessor, int count) in
                usedPropertyAccessors.Where(x => x.Count > 0))
            {
                var responseEntries = new List<FhirTransactionResponseEntry>();
                for (int j = 0; j < count; j++)
                {
                    FhirTransactionResponseEntry responseEntry = CreateResponseEntry(responseBundle.Entry[processedResponseItems + j]);
                    responseEntries.Add(responseEntry);
                }

                processedResponseItems += count;
                accessor.ResponseEntrySetter(context.Response, responseEntries);
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
