// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker
{
    /// <summary>
    /// Provides functionality to process the change feed.
    /// </summary>
    public class ChangeFeedProcessor : IChangeFeedProcessor
    {
        private readonly IChangeFeedRetrieveService _changeFeedRetrieveService;
        private readonly IFhirTransactionPipeline _fhirTransactionPipeline;
        private readonly ISyncStateService _syncStateService;
        private readonly IExceptionStore _exceptionStore;
        private readonly ILogger<ChangeFeedProcessor> _logger;

        public ChangeFeedProcessor(
            IChangeFeedRetrieveService changeFeedRetrieveService,
            IFhirTransactionPipeline fhirTransactionPipeline,
            ISyncStateService syncStateService,
            IExceptionStore exceptionStore,
            ILogger<ChangeFeedProcessor> logger)
        {
            EnsureArg.IsNotNull(changeFeedRetrieveService, nameof(changeFeedRetrieveService));
            EnsureArg.IsNotNull(fhirTransactionPipeline, nameof(fhirTransactionPipeline));
            EnsureArg.IsNotNull(syncStateService, nameof(syncStateService));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _changeFeedRetrieveService = changeFeedRetrieveService;
            _fhirTransactionPipeline = fhirTransactionPipeline;
            _syncStateService = syncStateService;
            _exceptionStore = exceptionStore;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task ProcessAsync(TimeSpan pollIntervalDuringCatchup, CancellationToken cancellationToken)
        {
            SyncState state = await _syncStateService.GetSyncStateAsync(cancellationToken);

            while (true)
            {
                // Retrieve the change feed for any changes.
                IReadOnlyList<ChangeFeedEntry> changeFeedEntries = await _changeFeedRetrieveService.RetrieveChangeFeedAsync(
                    state.SyncedSequence,
                    cancellationToken);

                if (!changeFeedEntries.Any())
                {
                    _logger.LogInformation("No new DICOM events to process.");

                    return;
                }

                long maxSequence = changeFeedEntries[^1].Sequence;

                // Process each change feed as a FHIR transaction.
                foreach (ChangeFeedEntry changeFeedEntry in changeFeedEntries)
                {
                    try
                    {
                        if (!(changeFeedEntry.Action == ChangeFeedAction.Create && changeFeedEntry.State == ChangeFeedState.Deleted))
                        {
                            await _fhirTransactionPipeline.ProcessAsync(changeFeedEntry, cancellationToken);
                            _logger.LogInformation("Succesfully process DICOM event with SequenceID: {sequenceId}", changeFeedEntry.Sequence);
                        }
                        else
                        {
                            _logger.LogInformation("Skip DICOM event with SequenceId {sequenceId} due to deletion before processing creation.", changeFeedEntry.Sequence);
                        }
                    }
                    catch (Exception ex)
                    {
                        string studyUid = changeFeedEntry.StudyInstanceUid;
                        string seriesUid = changeFeedEntry.SeriesInstanceUid;
                        string instanceUid = changeFeedEntry.SopInstanceUid;
                        long changeFeedSequence = changeFeedEntry.Sequence;

                        await _exceptionStore.WriteExceptionAsync(
                            changeFeedEntry,
                            ex,
                            ErrorType.IntransientError,
                            cancellationToken);

                        _logger.LogInformation("Failed to process DICOM event with SequenceID: {sequenceId}, StudyUid: {studyUid}, SeriesUid: {seriesUid}, instanceUid: {instanceUid}  and will not be retried further. Continuing to next event.", changeFeedEntry.Sequence, studyUid, seriesUid, instanceUid);
                    }
                }

                var newSyncState = new SyncState(maxSequence, Clock.UtcNow);

                await _syncStateService.UpdateSyncStateAsync(newSyncState, cancellationToken);

                _logger.LogInformation("Processed DICOM events sequenced {sequenceId}-{maxSequence}.", state.SyncedSequence + 1, maxSequence);

                state = newSyncState;

                await Task.Delay(pollIntervalDuringCatchup, cancellationToken);
            }
        }
    }
}
