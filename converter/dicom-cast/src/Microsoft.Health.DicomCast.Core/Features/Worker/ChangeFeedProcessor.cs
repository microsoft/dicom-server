// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Polly.Timeout;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker;

/// <summary>
/// Provides functionality to process the change feed.
/// </summary>
public class ChangeFeedProcessor : IChangeFeedProcessor
{
    internal const int DefaultLimit = 10;
    private static readonly Func<ILogger, IDisposable> LogProcessingDelegate = LoggerMessage.DefineScope("Processing change feed.");

    private readonly IChangeFeedRetrieveService _changeFeedRetrieveService;
    private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
    private readonly ISyncStateService _syncStateService;
    private readonly IExceptionStore _exceptionStore;
    private readonly TimeProvider _timeProvider;
    private readonly DicomCastConfiguration _configuration;
    private readonly ILogger<ChangeFeedProcessor> _logger;

    public ChangeFeedProcessor(
        IChangeFeedRetrieveService changeFeedRetrieveService,
        IFhirTransactionPipeline fhirTransactionPipeline,
        ISyncStateService syncStateService,
        IExceptionStore exceptionStore,
        IOptions<DicomCastConfiguration> dicomCastConfiguration,
        ILogger<ChangeFeedProcessor> logger)
        : this(changeFeedRetrieveService, fhirTransactionPipeline, syncStateService, exceptionStore, TimeProvider.System, dicomCastConfiguration, logger)
    { }

    internal ChangeFeedProcessor(
        IChangeFeedRetrieveService changeFeedRetrieveService,
        IFhirTransactionPipeline fhirTransactionPipeline,
        ISyncStateService syncStateService,
        IExceptionStore exceptionStore,
        TimeProvider timeProvider,
        IOptions<DicomCastConfiguration> dicomCastConfiguration,
        ILogger<ChangeFeedProcessor> logger)
    {
        _changeFeedRetrieveService = EnsureArg.IsNotNull(changeFeedRetrieveService, nameof(changeFeedRetrieveService));
        _fhirTransactionPipeline = EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));
        _syncStateService = EnsureArg.IsNotNull(syncStateService, nameof(syncStateService));
        _exceptionStore = EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));
        _configuration = EnsureArg.IsNotNull(dicomCastConfiguration?.Value, nameof(dicomCastConfiguration));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(TimeSpan pollIntervalDuringCatchup, CancellationToken cancellationToken)
    {
        using (LogProcessingDelegate(_logger))
        {
            SyncState state = await _syncStateService.GetSyncStateAsync(cancellationToken);

            while (true)
            {
                // Retrieve the change feed for any changes after checking the sequence number of the latest event
                long latest = await _changeFeedRetrieveService.RetrieveLatestSequenceAsync(cancellationToken);
                IReadOnlyList<ChangeFeedEntry> changeFeedEntries = await GetChangeFeedEntries(state, _configuration.Features.IgnoreJsonParsingErrors, cancellationToken);

                // If there are no events because nothing available, then skip processing for now
                // Note that there may be more events to read for API version v1 even if the Count < limit
                if (changeFeedEntries.Count == 0 && latest == state.SyncedSequence)
                {
                    _logger.LogInformation("No new DICOM events to process");
                    return;
                }

                // Otherwise, process any new entries and increment the sequence
                long maxSequence = changeFeedEntries.Count > 0 ? changeFeedEntries[^1].Sequence : state.SyncedSequence + DefaultLimit;
                await ProcessChangeFeedEntriesAsync(changeFeedEntries, cancellationToken);

                var newSyncState = new SyncState(maxSequence, _timeProvider.GetUtcNow());
                await _syncStateService.UpdateSyncStateAsync(newSyncState, cancellationToken);
                _logger.LogInformation("Processed DICOM events sequenced [{SequenceId}, {MaxSequence}]", state.SyncedSequence + 1, maxSequence);
                state = newSyncState;

                await Task.Delay(pollIntervalDuringCatchup, cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedEntries(SyncState state, bool ignoreJsonParsingErrors, CancellationToken cancellationToken)
    {
        try
        {
            return await _changeFeedRetrieveService.RetrieveChangeFeedAsync(
                            state.SyncedSequence,
                            DefaultLimit,
                            cancellationToken);
        }
        catch (JsonException)
        {
            if (!ignoreJsonParsingErrors)
            {
                throw;
            }

            return await GetChangeFeedEntriesOneByOne(state, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedEntriesOneByOne(SyncState state, CancellationToken cancellationToken)
    {
        long start = state.SyncedSequence;
        List<ChangeFeedEntry> changeFeedEntries = [];

        while (start < state.SyncedSequence + DefaultLimit)
        {
            try
            {
                var changeFeedEntry = await _changeFeedRetrieveService.RetrieveChangeFeedAsync(start, 1, cancellationToken);

                if (changeFeedEntry == null || changeFeedEntry.Count == 0)
                {
                    return changeFeedEntries.AsReadOnly();
                }

                start++;
                changeFeedEntries.Add(changeFeedEntry[0]);
            }
            catch (JsonException ex)
            {
                // ignore items that failed to parse
                _logger.LogError(ex, "Changefeed entry with SequenceId {SequenceId} failed to be parsed by the DicomWebClient", start);
                start++;
            }
        }

        return changeFeedEntries.AsReadOnly();
    }

    private async Task ProcessChangeFeedEntriesAsync(IEnumerable<ChangeFeedEntry> changeFeedEntries, CancellationToken cancellationToken)
    {
        // Process each change feed as a FHIR transaction.
        foreach (ChangeFeedEntry changeFeedEntry in changeFeedEntries)
        {
            try
            {
                if (!(changeFeedEntry.Action == ChangeFeedAction.Create && changeFeedEntry.State == ChangeFeedState.Deleted))
                {
                    await _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken);
                    _logger.LogInformation("Successfully processed DICOM event with SequenceID: {SequenceId}", changeFeedEntry.Sequence);
                }
                else
                {
                    _logger.LogInformation("Skip DICOM event with SequenceId {SequenceId} due to deletion before processing creation.", changeFeedEntry.Sequence);
                }
            }
            catch (Exception ex) when (ex is FhirNonRetryableException or DicomTagException or TimeoutRejectedException)
            {
                string studyInstanceUid = changeFeedEntry.StudyInstanceUid;
                string seriesInstanceUid = changeFeedEntry.SeriesInstanceUid;
                string sopInstanceUid = changeFeedEntry.SopInstanceUid;
                long changeFeedSequence = changeFeedEntry.Sequence;

                ErrorType errorType = ErrorType.FhirError;

                if (ex is DicomTagException)
                {
                    errorType = ErrorType.DicomError;
                }
                else if (ex is TimeoutRejectedException)
                {
                    errorType = ErrorType.TransientFailure;
                }

                await _exceptionStore.WriteExceptionAsync(changeFeedEntry, ex, errorType, cancellationToken);

                _logger.LogError(
                    "Failed to process DICOM event with SequenceID: {SequenceId}, StudyUid: {StudyInstanceUid}, SeriesUid: {SeriesInstanceUid}, instanceUid: {SopInstanceUid}  and will not be retried further. Continuing to next event.",
                    changeFeedSequence,
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid);
            }
        }
    }
}
