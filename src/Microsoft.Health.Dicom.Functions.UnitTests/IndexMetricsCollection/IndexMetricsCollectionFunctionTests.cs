// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Functions.IndexMetricsCollection;
using Microsoft.Health.Dicom.Functions.IndexMetricsCollection.Telemetry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.IndexMetricsCollection;

public class IndexMetricsCollectionFunctionTests
{
    private readonly IndexMetricsCollectionFunction _collectionFunction;
    private readonly IIndexDataStore _indexStore;
    private readonly IndexMetricsCollectionMeter _meter;
    private List<Metric> _exportedItems;
    private MeterProvider _meterProvider;
    private readonly TimerInfo _timer;

    public IndexMetricsCollectionFunctionTests()
    {
        _meter = new IndexMetricsCollectionMeter();
        _indexStore = Substitute.For<IIndexDataStore>();
        _collectionFunction = new IndexMetricsCollectionFunction(
            _indexStore,
            Options.Create(new FeatureConfiguration { EnableExternalStore = true, }),
            _meter);
        _timer = Substitute.For<TimerInfo>(default, default, default);
    }

    private void InitializeMetricExporter()
    {
        _exportedItems = new List<Metric>();
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter($"{OpenTelemetryLabels.BaseMeterName}.{IndexMetricsCollectionMeter.MeterName}")
            .AddInMemoryExporter(_exportedItems)
            .Build();
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_ThenIndexMetricsCollectionsCompletedCounterIsIncremented()
    {
        InitializeMetricExporter();
        _indexStore.GetIndexedFilePropertiesAsync().ReturnsForAnyArgs(new IndexedFileProperties());

        await _collectionFunction.Run(_timer, NullLogger.Instance);

        _meterProvider.ForceFlush();
        Assert.Single(_exportedItems);
        Assert.Equal(nameof(_meter.IndexMetricsCollectionsCompletedCounter), _exportedItems[0].Name);
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRunException_ThenIndexMetricsCollectionsCompletedCounterIsNotIncremented()
    {
        InitializeMetricExporter();
        _indexStore.GetIndexedFilePropertiesAsync().ThrowsForAnyArgs(new Exception());

        await Assert.ThrowsAsync<Exception>(async () => await _collectionFunction.Run(_timer, NullLogger.Instance));

        _meterProvider.ForceFlush();
        Assert.Empty(_exportedItems);
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_CollectionExecutedWhenExternalStoreEnabled()
    {
        InitializeMetricExporter();
        _indexStore.GetIndexedFilePropertiesAsync().ReturnsForAnyArgs(new IndexedFileProperties());

        await _collectionFunction.Run(_timer, NullLogger.Instance);

        await _indexStore.ReceivedWithAnyArgs(1).GetIndexedFilePropertiesAsync();
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_CollectionNotExecutedWhenExternalStoreNotEnabled()
    {
        InitializeMetricExporter();
        _indexStore.GetIndexedFilePropertiesAsync().ReturnsForAnyArgs(new IndexedFileProperties());
        var collectionFunctionWihtoutExternalStore = new IndexMetricsCollectionFunction(
            _indexStore,
            Options.Create(new FeatureConfiguration { EnableExternalStore = false, }),
            _meter);

        await collectionFunctionWihtoutExternalStore.Run(_timer, NullLogger.Instance);

        await _indexStore.DidNotReceiveWithAnyArgs().GetIndexedFilePropertiesAsync();
    }
}
