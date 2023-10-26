// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.BackgroundServices;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Models.Delete;
using NSubstitute;
using NSubstitute.Core;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.BackgroundServices;

public sealed class DeletedInstanceCleanupWorkerTests : IDisposable
{
    // Use a unique meter name to prevent conflicts with other tests
    private static readonly string MeterName = Guid.NewGuid().ToString();

    private readonly DeletedInstanceCleanupWorker _deletedInstanceCleanupWorker;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IDeleteService _deleteService = Substitute.For<IDeleteService>();
    private readonly DeleteMeter _deleteMeter = new(MeterName, "1.0");
    private readonly MeterProvider _meterProvider;
    private readonly List<Metric> _metrics = new();
    private const int BatchSize = 10;

    public DeletedInstanceCleanupWorkerTests()
    {
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(MeterName)
            .AddInMemoryExporter(_metrics)
            .Build();

        DeletedInstanceCleanupConfiguration config = new()
        {
            BatchSize = BatchSize,
            PollingInterval = TimeSpan.FromMilliseconds(100),
        };

        _deletedInstanceCleanupWorker = new DeletedInstanceCleanupWorker(
            _deleteService,
            _deleteMeter,
            Options.Create(config),
            NullLogger<DeletedInstanceCleanupWorker>.Instance);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(11, 2)]
    [InlineData(19, 2)]
    [InlineData(20, 3)]
    [InlineData(21, 3)]
    public async Task GivenANumberOfDeletedEntriesAndBatchSize_WhenCallingExecute_ThenDeleteShouldBeCalledCorrectNumberOfTimes(int numberOfDeletedInstances, int expectedDeleteCount)
    {
        _deleteService
            .CleanupDeletedInstancesAsync()
            .ReturnsForAnyArgs(GetDeleteSummary);

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);
        await _deleteService.ReceivedWithAnyArgs(expectedDeleteCount).CleanupDeletedInstancesAsync();

        DeleteSummary GetDeleteSummary(CallInfo callInfo)
        {
            var returnValue = Math.Min(numberOfDeletedInstances, BatchSize);
            numberOfDeletedInstances = Math.Max(numberOfDeletedInstances - BatchSize, 0);

            if (numberOfDeletedInstances == 0)
            {
                _cancellationTokenSource.Cancel();
            }

            return new DeleteSummary { ProcessedCount = returnValue, Success = true };
        }
    }

    [Fact]
    public async Task GivenANotReadyDataStore_WhenCallingExecute_ThenNothingShouldHappen()
    {
        int iterations = 3;
        int count = 0;
        _deleteService
            .CleanupDeletedInstancesAsync()
            .ReturnsForAnyArgs(GetDeleteSummary);

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);
        await _deleteService.ReceivedWithAnyArgs(4).CleanupDeletedInstancesAsync();

        DeleteSummary GetDeleteSummary(CallInfo callInfo)
        {
            if (count < iterations)
            {
                count++;
                throw new DataStoreNotReadyException("Datastore not ready");
            }

            _cancellationTokenSource.Cancel();

            return new DeleteSummary { ProcessedCount = 1, Success = true };
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GivenAnyDeletionResult_WhenFinishedDeleting_ThenSendMetrics(bool success)
    {
        DateTimeOffset oldest = DateTimeOffset.UtcNow.AddDays(-5);
        const int TotalExhausted = 42;

        _deleteService
            .CleanupDeletedInstancesAsync()
            .ReturnsForAnyArgs(GetDeleteSummary);

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);

        // Force the meter provides to emit the metrics earlier than they might otherwise
        _meterProvider.ForceFlush();
        Assert.Equal(2, _metrics.Count);
        AssertLongCounter(oldest.ToUnixTimeSeconds(), _metrics[0]);
        AssertLongCounter(TotalExhausted, _metrics[1]);

        DeleteSummary GetDeleteSummary(CallInfo callInfo)
        {
            _cancellationTokenSource.Cancel();
            return new DeleteSummary
            {
                Metrics = new DeleteMetrics
                {
                    OldestDeletion = oldest,
                    TotalExhaustedRetries = TotalExhausted,
                },
                ProcessedCount = BatchSize - 1,
                Success = success,
            };
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GivenNoMetrics_WhenFinishedDeleting_ThenSkipSendingMetrics(bool success)
    {
        _deleteService
            .CleanupDeletedInstancesAsync()
            .ReturnsForAnyArgs(GetDeleteSummary);

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);

        _meterProvider.ForceFlush();
        Assert.Empty(_metrics);

        DeleteSummary GetDeleteSummary(CallInfo callInfo)
        {
            _cancellationTokenSource.Cancel();
            return new DeleteSummary
            {
                Metrics = null,
                ProcessedCount = BatchSize - 1,
                Success = success,
            };
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        _meterProvider.Dispose();
    }

    private static void AssertLongCounter(long value, Metric actual)
    {
        Assert.Equal(MetricType.LongSum, actual.MetricType);

        MetricPointsAccessor.Enumerator t = actual.GetMetricPoints().GetEnumerator();

        Assert.True(t.MoveNext());
        Assert.Equal(value, t.Current.GetSumLong());
        Assert.False(t.MoveNext());
    }
}
