// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Polly;
using Polly.Timeout;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

/// <summary>
/// Provides functionality to build and process response of a FHIR transaction.
/// </summary>
public class FhirTransactionPipeline : IFhirTransactionPipeline
{
    private readonly IEnumerable<IFhirTransactionPipelineStep> _fhirTransactionPipelines;
    private readonly IFhirTransactionExecutor _fhirTransactionExecutor;

    private readonly IReadOnlyList<FhirTransactionRequestResponsePropertyAccessor> _requestResponsePropertyAccessors;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IExceptionStore _exceptionStore;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private readonly ILogger<FhirTransactionPipeline> _logger;

    public FhirTransactionPipeline(
        IEnumerable<IFhirTransactionPipelineStep> fhirTransactionPipelines,
        IFhirTransactionRequestResponsePropertyAccessors fhirTransactionRequestResponsePropertyAccessors,
        IFhirTransactionExecutor fhirTransactionExecutor,
        IExceptionStore exceptionStore,
        IOptions<RetryConfiguration> retryConfiguration,
        ILogger<FhirTransactionPipeline> logger)
    {
        _fhirTransactionPipelines = EnsureArg.IsNotNull(fhirTransactionPipelines, nameof(fhirTransactionPipelines));
        _requestResponsePropertyAccessors = EnsureArg.IsNotNull(fhirTransactionRequestResponsePropertyAccessors?.PropertyAccessors, nameof(fhirTransactionRequestResponsePropertyAccessors));
        _fhirTransactionExecutor = EnsureArg.IsNotNull(fhirTransactionExecutor, nameof(fhirTransactionExecutor));
        _exceptionStore = EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));
        EnsureArg.IsNotNull(retryConfiguration?.Value, nameof(retryConfiguration));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        _timeoutPolicy = Policy.TimeoutAsync(retryConfiguration.Value.TotalRetryDuration);
        _retryPolicy = Policy
            .Handle<RetryableException>()
            .WaitAndRetryForeverAsync(
                (retryAttempt, exception, context) =>
                {
                    return TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, retryAttempt)));
                },
                (exception, retryCount, timeSpan, context) =>
                {
                    var changeFeedEntry = (ChangeFeedEntry)context[nameof(ChangeFeedEntry)];

                    return _exceptionStore.WriteRetryableExceptionAsync(
                        changeFeedEntry,
                        retryCount,
                        timeSpan,
                        exception);
                });
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(changeFeedEntry, nameof(changeFeedEntry));

        try
        {
            var context = new Context
            {
                { nameof(ChangeFeedEntry), changeFeedEntry },
            };

            await _timeoutPolicy.WrapAsync(_retryPolicy).ExecuteAsync(
                async (ctx, tkn) =>
                {
                    try
                    {
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

                        _logger.LogInformation("Successfully processed the change feed entry.");
                    }
                    catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new RetryableException(ex);
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new RetryableException(ex);
                    }
                },
                context,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancel requested
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encountered an exception while processing the change feed entry.");
            throw;
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
