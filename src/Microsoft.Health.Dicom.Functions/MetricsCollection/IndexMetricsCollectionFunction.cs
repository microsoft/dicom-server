// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Functions.MetricsCollection.Telemetry;
using Microsoft.Health.Functions.Extensions;

namespace Microsoft.Health.Dicom.Functions.MetricsCollection;

/// <summary>
///  A function for collecting index metrics
/// </summary>
public class IndexMetricsCollectionFunction
{
    private readonly IIndexDataStore _indexDataStore;
    private readonly IndexMetricsCollectionMeter _meter;
    private readonly bool _externalStoreEnabled;
    private readonly bool _enableDataPartitions;
    private const string RunFrequencyVariable = $"%{AzureFunctionsJobHost.RootSectionName}:DicomFunctions:{IndexMetricsCollectionOptions.SectionName}:{nameof(IndexMetricsCollectionOptions.Frequency)}%";

    public IndexMetricsCollectionFunction(
        IIndexDataStore indexDataStore,
        IOptions<FeatureConfiguration> featureConfiguration,
        IndexMetricsCollectionMeter meter)
    {
        EnsureArg.IsNotNull(featureConfiguration, nameof(featureConfiguration));
        _indexDataStore = EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        _meter = EnsureArg.IsNotNull(meter, nameof(meter));
        _externalStoreEnabled = featureConfiguration.Value.EnableExternalStore;
        _enableDataPartitions = featureConfiguration.Value.EnableDataPartitions;
    }

    /// <summary>
    /// Asynchronously collects index metrics.
    /// </summary>
    /// <param name="invocationTimer">The timer which tracks the invocation schedule.</param>
    /// <param name="log">A diagnostic logger.</param>
    /// <returns>A task that represents the asynchronous metrics collection operation.</returns>
    [FunctionName(nameof(IndexMetricsCollectionFunction))]
    public async Task Run(
        [TimerTrigger(RunFrequencyVariable)] TimerInfo invocationTimer,
        ILogger log)
    {
        EnsureArg.IsNotNull(invocationTimer, nameof(invocationTimer));
        EnsureArg.IsNotNull(log, nameof(log));
        if (!_externalStoreEnabled)
        {
            log.LogInformation("External store is not enabled. Skipping index metrics collection.");
            return;
        }

        log.LogInformation("Collecting a daily summation starting. At: {Timestamp}", DateTime.UtcNow);
        if (invocationTimer.IsPastDue)
        {
            log.LogWarning("Current function invocation is running late.");
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        IndexedFileProperties indexedFileProperties = await _indexDataStore.GetIndexedFileMetricsAsync();

        stopwatch.Stop();

        log.LogInformation(
            "DICOM telemetry - TotalFilesIndexed: {TotalFilesIndexed} , TotalByesIndexed: {TotalContentLengthIndexed} ,  with ExternalStoreEnabled: {ExternalStoreEnabled} and DataPartitionsEnabled: {PartitionsEnabled}",
            indexedFileProperties.TotalIndexed,
            indexedFileProperties.TotalSum,
            _externalStoreEnabled,
            _enableDataPartitions);

        _meter.IndexMetricsCollectionsCompletedCounter.Add(1, IndexMetricsCollectionMeter.CreateTelemetryDimension(_externalStoreEnabled, _enableDataPartitions));

        log.LogInformation(
            "Collecting a daily summation time taken: {ElapsedTime} ms with ExternalStoreEnabled: {ExternalStoreEnabled} and DataPartitionsEnabled: {PartitionsEnabled}",
            stopwatch.ElapsedMilliseconds,
            _externalStoreEnabled,
            _enableDataPartitions);
    }
}