// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Core.Internal;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.Core.Features.Worker;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Microsoft.Health.Test.Utilities;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker
{
    public class ChangeFeedProcessorTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly IChangeFeedRetrieveService _changeFeedRetrieveService = Substitute.For<IChangeFeedRetrieveService>();
        private readonly IFhirTransactionPipeline _fhirTransactionPipeline = Substitute.For<IFhirTransactionPipeline>();
        private readonly ISyncStateService _syncStateService = Substitute.For<ISyncStateService>();
        private readonly ChangeFeedProcessor _changeFeedProcessor;

        public ChangeFeedProcessorTests()
        {
            _changeFeedProcessor = new ChangeFeedProcessor(
                _changeFeedRetrieveService,
                _fhirTransactionPipeline,
                _syncStateService,
                NullLogger<ChangeFeedProcessor>.Instance);

            SetupSyncState();
        }

        [Fact]
        public async Task GivenMultipleChangeFeedEntries_WhenProcessed_ThenEachChangeFeedEntryShouldBeProcessed()
        {
            ChangeFeedEntry[] changeFeeds = new[]
            {
                ChangeFeedGenerator.Generate(1),
                ChangeFeedGenerator.Generate(2),
                ChangeFeedGenerator.Generate(3),
            };

            _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, DefaultCancellationToken).Returns(changeFeeds);

            await ExecuteProcessAsync();

            await _fhirTransactionPipeline.ReceivedWithAnyArgs(3).ProcessAsync(default, default);
            await _fhirTransactionPipeline.Received().ProcessAsync(changeFeeds[0], DefaultCancellationToken);
            await _fhirTransactionPipeline.Received().ProcessAsync(changeFeeds[1], DefaultCancellationToken);
            await _fhirTransactionPipeline.Received().ProcessAsync(changeFeeds[2], DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenMultipleChangeFeedEntries_WhenProcessing_ThenItShouldProcessAllPendinChangeFeedEntries()
        {
            ChangeFeedEntry[] changeFeeds1 = new[]
            {
                ChangeFeedGenerator.Generate(1),
            };

            ChangeFeedEntry[] changeFeeds2 = new[]
            {
                ChangeFeedGenerator.Generate(2),
            };

            _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, DefaultCancellationToken).Returns(changeFeeds1);
            _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, DefaultCancellationToken).Returns(changeFeeds2);
            _changeFeedRetrieveService.RetrieveChangeFeedAsync(2, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

            await ExecuteProcessAsync();

            await _fhirTransactionPipeline.ReceivedWithAnyArgs(2).ProcessAsync(default, default);
            await _fhirTransactionPipeline.Received().ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
            await _fhirTransactionPipeline.Received().ProcessAsync(changeFeeds2[0], DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenMultipleChangeFeedEntries_WhenProcessing_ThenPollIntervalShouldBeHonored()
        {
            TimeSpan pollIntervalDuringCatchup = TimeSpan.FromMilliseconds(50);

            ChangeFeedEntry[] changeFeeds1 = new[]
            {
                ChangeFeedGenerator.Generate(1),
            };

            ChangeFeedEntry[] changeFeeds2 = new[]
            {
                ChangeFeedGenerator.Generate(2),
            };

            _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, DefaultCancellationToken).Returns(changeFeeds1);
            _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, DefaultCancellationToken).Returns(changeFeeds2);
            _changeFeedRetrieveService.RetrieveChangeFeedAsync(2, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

            var stopwatch = new Stopwatch();

            _fhirTransactionPipeline.When(processor => processor.ProcessAsync(changeFeeds1[0], DefaultCancellationToken)).Do(_ => stopwatch.Start());
            _fhirTransactionPipeline.When(processor => processor.ProcessAsync(changeFeeds2[0], DefaultCancellationToken)).Do(_ => stopwatch.Stop());

            // Execute Process when no poll interval is defined.
            await ExecuteProcessAsync();

            // Using stopwatch.Elapsed to get total time elapsed when no poll interval is defined.
            TimeSpan totalTimeTakenWithNoPollInterval = stopwatch.Elapsed;

            stopwatch.Reset();

            // Execute process when poll interval is defined.
            await ExecuteProcessAsync(pollIntervalDuringCatchup);

            // Using stopwatch.Elapsed to get total time elapsed when poll interval is defined.
            TimeSpan totalTimeTakenWithPollInterval = stopwatch.Elapsed;

            Assert.True(totalTimeTakenWithPollInterval >= totalTimeTakenWithNoPollInterval);
        }

        [Fact]
        public async Task GivenNoChangeFeed_WhenProcessed_ThenSyncStateShouldNotBeUpdated()
        {
            _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, DefaultCancellationToken).Returns(new ChangeFeedEntry[0]);

            await ExecuteProcessAsync();

            await _syncStateService.ReceivedWithAnyArgs(0).UpdateSyncStateAsync(default, default);
        }

        [Fact]
        public async Task GivenAllChangeFeedEntriesAreSuccess_WhenProcessed_ThenSyncStateShouldBeUpdated()
        {
            const long expectedSequence = 10;

            ChangeFeedEntry[] changeFeeds = new[]
            {
                ChangeFeedGenerator.Generate(expectedSequence),
            };

            _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, DefaultCancellationToken).Returns(changeFeeds);

            var instant = new DateTimeOffset(2020, 6, 1, 15, 30, 25, TimeSpan.FromHours(-8));

            using (Mock.Property(() => ClockResolver.UtcNowFunc, () => instant))
            {
                await ExecuteProcessAsync();
            }

            await _syncStateService.Received(1)
                .UpdateSyncStateAsync(Arg.Is<SyncState>(syncState => syncState != null && syncState.SyncedSequence == expectedSequence && syncState.SyncedDate == instant), DefaultCancellationToken);
        }

        private void SetupSyncState(long syncedSequence = 0, DateTimeOffset? syncedDate = null)
        {
            var syncState = new SyncState(syncedSequence, syncedDate == null ? DateTimeOffset.MinValue : syncedDate.Value);

            _syncStateService.GetSyncStateAsync(DefaultCancellationToken).Returns(syncState);
        }

        private async Task ExecuteProcessAsync(TimeSpan? pollIntervalDuringCatchup = null)
        {
            if (pollIntervalDuringCatchup == null)
            {
                pollIntervalDuringCatchup = TimeSpan.Zero;
            }

            await _changeFeedProcessor.ProcessAsync(pollIntervalDuringCatchup.Value, DefaultCancellationToken);
        }
    }
}
