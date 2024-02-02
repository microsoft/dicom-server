// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Functions.MetricsCollection;
using Microsoft.Health.Dicom.Functions.MetricsCollection.Telemetry;
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
    private readonly List<Metric> _exportedItems;
    private readonly MeterProvider _meterProvider;
    private readonly TimerInfo _timer;

    public IndexMetricsCollectionFunctionTests()
    {
        string meterName = Guid.NewGuid().ToString();
        _meter = new IndexMetricsCollectionMeter(meterName);
        _exportedItems = new List<Metric>();
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddInMemoryExporter(_exportedItems)
            .Build();
        _indexStore = Substitute.For<IIndexDataStore>();
        _collectionFunction = new IndexMetricsCollectionFunction(
            _indexStore,
            Options.Create(new FeatureConfiguration { EnableExternalStore = true, }),
            _meter);
        _timer = Substitute.For<TimerInfo>(default, default, default);
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRunException_ThenIndexMetricsCollectionsCompletedCounterIsIncremented()
    {
        _indexStore.GetIndexedFileMetricsAsync().ThrowsForAnyArgs(new Exception());

        await Assert.ThrowsAsync<Exception>(() => _collectionFunction.Run(_timer, NullLogger.Instance));

        _meterProvider.ForceFlush();
        AssertMetricEmitted(_meter.IndexMetricsCollectionsCompletedCounter.Name, _exportedItems, expectedSucceededTagValue: false);
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_CollectionExecutedWhenExternalStoreEnabled()
    {
        _indexStore.GetIndexedFileMetricsAsync().ReturnsForAnyArgs(new IndexedFileProperties());

        await _collectionFunction.Run(_timer, NullLogger.Instance);

        await _indexStore.ReceivedWithAnyArgs(1).GetIndexedFileMetricsAsync();

        _meterProvider.ForceFlush();
        AssertMetricEmitted(_meter.IndexMetricsCollectionsCompletedCounter.Name, _exportedItems);
    }

    [Fact]
    public async Task GivenIndexMetricsCollectionFunction_WhenRun_CollectionNotExecutedWhenExternalStoreNotEnabled()
    {
        _indexStore.GetIndexedFileMetricsAsync().ReturnsForAnyArgs(new IndexedFileProperties());
        var collectionFunctionWihtoutExternalStore = new IndexMetricsCollectionFunction(
            _indexStore,
            Options.Create(new FeatureConfiguration { EnableExternalStore = false, }),
            _meter);

        await collectionFunctionWihtoutExternalStore.Run(_timer, NullLogger.Instance);

        await _indexStore.DidNotReceiveWithAnyArgs().GetIndexedFileMetricsAsync();
        _meterProvider.ForceFlush();
        Assert.Empty(_exportedItems);
    }

    private static void AssertMetricEmitted(
        string metricName,
        List<Metric> exportedItems,
        bool expectedSucceededTagValue = true)
    {
        Assert.NotEmpty(exportedItems);
        Collection<MetricPoint> points = exportedItems.GetMetricPoints(metricName);
        Assert.Single(points);

        Dictionary<string, object> tags = points[0].GetTags();
        Assert.Equal(expectedSucceededTagValue, tags["CollectionSucceeded"]);
    }
}
