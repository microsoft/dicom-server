// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.State;
using Microsoft.Health.DicomCast.Core.Features.Worker;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker;

public class ChangeFeedProcessorTests
{
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

    private readonly IChangeFeedRetrieveService _changeFeedRetrieveService = Substitute.For<IChangeFeedRetrieveService>();
    private readonly IFhirTransactionPipeline _fhirTransactionPipeline = Substitute.For<IFhirTransactionPipeline>();
    private readonly ISyncStateService _syncStateService = Substitute.For<ISyncStateService>();
    private readonly IExceptionStore _exceptionStore = Substitute.For<IExceptionStore>();
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
    private readonly IOptions<DicomCastConfiguration> _config = Substitute.For<IOptions<DicomCastConfiguration>>();
    private readonly DicomCastConfiguration _dicomCastConfiguration = new DicomCastConfiguration();
    private readonly ChangeFeedProcessor _changeFeedProcessor;

    public ChangeFeedProcessorTests()
    {
        _dicomCastConfiguration.Features.IgnoreJsonParsingErrors = true;
        _config.Value.Returns(_dicomCastConfiguration);

        _changeFeedProcessor = new ChangeFeedProcessor(
            _changeFeedRetrieveService,
            _fhirTransactionPipeline,
            _syncStateService,
            _exceptionStore,
            _timeProvider,
            _config,
            NullLogger<ChangeFeedProcessor>.Instance);

        SetupSyncState();
    }

    [Fact]
    public async Task GivenMultipleChangeFeedEntries_WhenProcessed_ThenEachChangeFeedEntryShouldBeProcessed()
    {
        const long Latest = 3L;
        ChangeFeedEntry[] changeFeeds = new[]
        {
            ChangeFeedGenerator.Generate(1),
            ChangeFeedGenerator.Generate(2),
            ChangeFeedGenerator.Generate(Latest),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(Latest);

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(2).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(2).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.ReceivedWithAnyArgs(3).ProcessAsync(default, default);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds[0], DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds[1], DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds[2], DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenMalformedChangeFeedEntries_WhenProcessed_BadEntryShouldBeSkipped()
    {
        const long Latest = 3L;
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };
        ChangeFeedEntry[] changeFeeds2 = new[]
        {
            ChangeFeedGenerator.Generate(Latest),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(Latest);

        // call to retrieve batch has json exception
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).ThrowsAsync(new JsonException());

        // get the items individually from the change feed
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, 1, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, 1, DefaultCancellationToken).ThrowsAsync(new JsonException());
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(2, 1, DefaultCancellationToken).Returns(changeFeeds2);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(Latest, 1, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(2).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(6).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, 1, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(1, 1, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(2, 1, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(Latest, 1, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.ReceivedWithAnyArgs(2).ProcessAsync(default, default);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds2[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenMalformedChangeFeedEntries_WhenProcessedAndNotIgnoringErrors_ExceptionIsThrown()
    {
        const long Latest = 3L;
        _dicomCastConfiguration.Features.IgnoreJsonParsingErrors = false;

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(Latest);

        // call to retrieve batch has json exception
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).ThrowsAsync(new JsonException());

        // Act
        await Assert.ThrowsAsync<JsonException>(() => ExecuteProcessAsync());
    }

    [Fact]
    public async Task GivenMultipleChangeFeedEntries_WhenProcessing_ThenItShouldProcessAllPendinChangeFeedEntries()
    {
        const long Latest = 2L;
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        ChangeFeedEntry[] changeFeeds2 = new[]
        {
            ChangeFeedGenerator.Generate(Latest),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(Latest);

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds2);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(3).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(3).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.ReceivedWithAnyArgs(2).ProcessAsync(default, default);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds2[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenSkippedSequenceNumbers_WhenProcessing_ThenSkipAheadByLimit()
    {
        const long PageOneEnd = 5L;
        const long Latest = PageOneEnd + ChangeFeedProcessor.DefaultLimit + 1; // Fall onto the next page
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
            ChangeFeedGenerator.Generate(PageOneEnd),
        };

        ChangeFeedEntry[] changeFeeds3 = new[]
        {
            ChangeFeedGenerator.Generate(Latest),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(PageOneEnd, Latest, Latest, Latest);

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(PageOneEnd, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(PageOneEnd + ChangeFeedProcessor.DefaultLimit, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds3);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(4).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(4).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.RetrieveChangeFeedAsync(PageOneEnd, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.RetrieveChangeFeedAsync(PageOneEnd + ChangeFeedProcessor.DefaultLimit, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.RetrieveChangeFeedAsync(Latest, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.ReceivedWithAnyArgs(3).ProcessAsync(default, default);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[1], DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds3[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task WhenThrowUnhandledError_ErrorThrown()
    {
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _fhirTransactionPipeline.When(pipeline => pipeline.ProcessAsync(Arg.Any<ChangeFeedEntry>(), Arg.Any<CancellationToken>())).Do(pipeline => { throw new Exception(); });

        // Act
        await Assert.ThrowsAsync<Exception>(() => ExecuteProcessAsync());

        // Assert
        await _changeFeedRetrieveService.Received(1).RetrieveLatestSequenceAsync(DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task WhenThrowTimeoutRejectedException_ExceptionNotThrown()
    {
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(1L);

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        _fhirTransactionPipeline.When(pipeline => pipeline.ProcessAsync(Arg.Any<ChangeFeedEntry>(), Arg.Any<CancellationToken>())).Do(pipeline => { throw new TimeoutRejectedException(); });

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(2).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(2).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
    }

    [Theory]
    [InlineData(nameof(DicomTagException))]
    [InlineData(nameof(MissingRequiredDicomTagException))]
    public async Task WhenThrowDicomTagException_ExceptionNotThrown(string exception)
    {
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(1L);

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        _fhirTransactionPipeline.When(pipeline => pipeline.ProcessAsync(Arg.Any<ChangeFeedEntry>(), Arg.Any<CancellationToken>())).Do(pipeline => { ThrowDicomTagException(exception); });

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(2).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(2).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task WhenMissingRequiredDicomTagException_ExceptionNotThrown()
    {
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(1L);

        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        _fhirTransactionPipeline.When(pipeline => pipeline.ProcessAsync(Arg.Any<ChangeFeedEntry>(), Arg.Any<CancellationToken>())).Do(pipeline => { throw new MissingRequiredDicomTagException(nameof(DicomTag.PatientID)); });

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(2).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(2).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task WhenThrowFhirNonRetryableException_ExceptionNotThrown()
    {
        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(1L);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        _fhirTransactionPipeline.When(pipeline => pipeline.ProcessAsync(Arg.Any<ChangeFeedEntry>(), Arg.Any<CancellationToken>())).Do(pipeline => { throw new FhirNonRetryableException("exception"); });

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _changeFeedRetrieveService.Received(2).RetrieveLatestSequenceAsync(DefaultCancellationToken);

        await _changeFeedRetrieveService.ReceivedWithAnyArgs(2).RetrieveChangeFeedAsync(default, default, default);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);

        await _fhirTransactionPipeline.Received(1).ProcessAsync(changeFeeds1[0], DefaultCancellationToken);
    }

    [Fact]
    public async Task GivenMultipleChangeFeedEntries_WhenProcessing_ThenPollIntervalShouldBeHonored()
    {
        var pollIntervalDuringCatchup = TimeSpan.FromMilliseconds(50);

        ChangeFeedEntry[] changeFeeds1 = new[]
        {
            ChangeFeedGenerator.Generate(1),
        };

        ChangeFeedEntry[] changeFeeds2 = new[]
        {
            ChangeFeedGenerator.Generate(2),
        };

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(2L);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds1);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(1, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds2);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(2, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

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
    public async Task GivenDefaultState_WhenProcessed_ThenSyncStateShouldNotBeUpdated()
    {
        // Arrange
        _syncStateService.GetSyncStateAsync(DefaultCancellationToken).Returns(SyncState.CreateInitialSyncState());
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(0L);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _syncStateService.Received(1).GetSyncStateAsync(DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveLatestSequenceAsync(DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _fhirTransactionPipeline.DidNotReceiveWithAnyArgs().ProcessAsync(default, default);
        await _syncStateService.DidNotReceiveWithAnyArgs().UpdateSyncStateAsync(default, default);
    }

    [Fact]
    public async Task GivenNoChangeFeed_WhenProcessed_ThenSyncStateShouldNotBeUpdated()
    {
        const long Sequence = 27L;

        // Arrange
        _syncStateService.GetSyncStateAsync(DefaultCancellationToken).Returns(new SyncState(Sequence, DateTimeOffset.UtcNow));
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(Sequence);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(Sequence, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(Array.Empty<ChangeFeedEntry>());

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _syncStateService.Received(1).GetSyncStateAsync(DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveLatestSequenceAsync(DefaultCancellationToken);
        await _changeFeedRetrieveService.Received(1).RetrieveChangeFeedAsync(Sequence, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken);
        await _fhirTransactionPipeline.DidNotReceiveWithAnyArgs().ProcessAsync(default, default);
        await _syncStateService.DidNotReceiveWithAnyArgs().UpdateSyncStateAsync(default, default);
    }

    [Fact]
    public async Task GivenAllChangeFeedEntriesAreSuccess_WhenProcessed_ThenSyncStateShouldBeUpdated()
    {
        const long expectedSequence = 10;

        ChangeFeedEntry[] changeFeeds = [ChangeFeedGenerator.Generate(expectedSequence)];

        // Arrange
        _changeFeedRetrieveService.RetrieveLatestSequenceAsync(DefaultCancellationToken).Returns(expectedSequence);
        _changeFeedRetrieveService.RetrieveChangeFeedAsync(0, ChangeFeedProcessor.DefaultLimit, DefaultCancellationToken).Returns(changeFeeds);

        var instant = DateTimeOffset.UtcNow.AddHours(1);
        _timeProvider.SetUtcNow(instant);

        // Act
        await ExecuteProcessAsync();

        // Assert
        await _syncStateService
            .Received(1)
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

    private static void ThrowDicomTagException(string exception)
    {
        if (exception.Equals(nameof(DicomTagException)))
        {
            throw new DicomTagException("exception");
        }
        else if (exception.Equals(nameof(MissingRequiredDicomTagException)))
        {
            throw new MissingRequiredDicomTagException(nameof(DicomTag.PatientID));
        }
    }
}
