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
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.BackgroundServices;

public sealed class DeletedInstanceCleanupWorkerTests : IDisposable
{
    private readonly string _meterName = Guid.NewGuid().ToString(); // Use a unique meter name to prevent conflicts with other tests
    private readonly DeletedInstanceCleanupWorker _deletedInstanceCleanupWorker;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IDeleteService _deleteService = Substitute.For<IDeleteService>();
    private readonly DeleteMeter _deleteMeter;
    private readonly MeterProvider _meterProvider;
    private readonly List<Metric> _metrics = new();
    private const int BatchSize = 10;

    public DeletedInstanceCleanupWorkerTests()
    {
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(_meterName)
            .AddInMemoryExporter(_metrics)
            .Build();

        _deleteMeter = new(_meterName, "1.0");
        DeletedInstanceCleanupConfiguration config = new()
        {
            BatchSize = BatchSize,
            PollingInterval = TimeSpan.FromMilliseconds(100),
        };

        _deletedInstanceCleanupWorker = new DeletedInstanceCleanupWorker(_deleteService, _deleteMeter, Options.Create(config), NullLogger<DeletedInstanceCleanupWorker>.Instance);
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
        _deleteService.CleanupDeletedInstancesAsync().ReturnsForAnyArgs(
            x => GenerateCleanupDeletedInstancesAsyncResponse());

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);
        await _deleteService.ReceivedWithAnyArgs(expectedDeleteCount).CleanupDeletedInstancesAsync();

        (bool, int) GenerateCleanupDeletedInstancesAsyncResponse()
        {
            var returnValue = Math.Min(numberOfDeletedInstances, BatchSize);
            numberOfDeletedInstances = Math.Max(numberOfDeletedInstances - BatchSize, 0);

            if (numberOfDeletedInstances == 0)
            {
                _cancellationTokenSource.Cancel();
            }

            return (true, returnValue);
        }
    }

    [Fact]
    public async Task GivenANotReadyDataStore_WhenCallingExecute_ThenNothingShouldHappen()
    {
        int iterations = 3;
        int count = 0;
        _deleteService.CleanupDeletedInstancesAsync().ReturnsForAnyArgs(
            x => GenerateCleanupDeletedInstancesAsyncResponse());

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);
        await _deleteService.ReceivedWithAnyArgs(4).CleanupDeletedInstancesAsync();

        (bool, int) GenerateCleanupDeletedInstancesAsyncResponse()
        {
            if (count < iterations)
            {
                count++;
                throw new DataStoreNotReadyException("Datastore not ready");
            }

            _cancellationTokenSource.Cancel();

            return (true, 1);
        }
    }

    [Fact]
    public async Task GivenANotReadyDataStore_WhenFetchingMetrics_ThenNothingShouldHappen()
    {
        _deleteService
            .GetMetricsAsync(_cancellationTokenSource.Token)
            .Returns(x =>
            {
                _cancellationTokenSource.Cancel();
                return Task.FromException<DeleteMetrics>(new DataStoreNotReadyException("Not yet!"));
            });

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);
        await _deleteService.Received(1).GetMetricsAsync(_cancellationTokenSource.Token);
        await _deleteService.DidNotReceiveWithAnyArgs().CleanupDeletedInstancesAsync(default);
    }

    [Fact]
    public async Task GivenMetrics_WhenEmitting_ThenSendMetrics()
    {
        DateTimeOffset oldest = DateTimeOffset.UtcNow.AddDays(-5);
        const int TotalExhausted = 42;

        _deleteService
            .GetMetricsAsync(_cancellationTokenSource.Token)
            .Returns(x =>
            {
                _cancellationTokenSource.Cancel();
                return new DeleteMetrics { OldestDeletion = oldest, TotalExhaustedRetries = TotalExhausted };
            });

        await _deletedInstanceCleanupWorker.ExecuteAsync(_cancellationTokenSource.Token);

        // Force the meter provides to emit the metrics earlier than they might otherwise
        _meterProvider.ForceFlush();
        Assert.Equal(2, _metrics.Count);
        AssertLongCounter(oldest.ToUnixTimeSeconds(), _metrics[0]);
        AssertLongCounter(TotalExhausted, _metrics[1]);
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
