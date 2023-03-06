// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker;

public class DicomCastWorkerTests
{
    private const int DefaultNumberOfInvocations = 2;

    private readonly DicomCastWorkerConfiguration _dicomCastWorkerConfiguration = new DicomCastWorkerConfiguration();
    private readonly IChangeFeedProcessor _changeFeedProcessor;
    private readonly IHostApplicationLifetime _hostApplication;
    private readonly DicomCastWorker _dicomCastWorker;
    private readonly IFhirService _fhirService;
    private readonly DicomCastMeter _dicomCastMeter;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly CancellationToken _cancellationToken;

    private MeterProvider _meterProvider;
    private List<Metric> _exportedItems;

    public DicomCastWorkerTests()
    {
        _cancellationToken = _cancellationTokenSource.Token;

        _dicomCastWorkerConfiguration.PollInterval = TimeSpan.Zero;

        _changeFeedProcessor = Substitute.For<IChangeFeedProcessor>();

        _hostApplication = Substitute.For<IHostApplicationLifetime>();

        _fhirService = Substitute.For<IFhirService>();

        _dicomCastMeter = new DicomCastMeter();

        _dicomCastWorker = new DicomCastWorker(
            Options.Create(_dicomCastWorkerConfiguration),
            _changeFeedProcessor,
            NullLogger<DicomCastWorker>.Instance,
            _hostApplication,
            _fhirService,
            _dicomCastMeter);

        InitializeMetricExporter();
    }

    [Fact]
    public async Task GivenWorkerIsBeingCanceled_WhenExecuting_ThenWorkerShouldBeCancelled()
    {
        int invocationCount = 0;

        _changeFeedProcessor.When(processor => processor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken))
            .Do(_ =>
            {
                if (invocationCount++ == DefaultNumberOfInvocations)
                {
                    _cancellationTokenSource.Cancel();

                    throw new TaskCanceledException();
                }
            });

        await _dicomCastWorker.ExecuteAsync(_cancellationToken);

        await _changeFeedProcessor.Received(invocationCount).ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken);
    }

    [Fact]
    public async Task GivenWorkerIsBeingCanceled_WhenExecutingAndFailed_ThenProperMetricsisLogged()
    {
        _changeFeedProcessor.When(processor => processor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken))
            .Do(_ =>
            {
                throw new TaskCanceledException();
            });

        await _dicomCastWorker.ExecuteAsync(_cancellationToken);

        _meterProvider.ForceFlush();

        Assert.NotEmpty(_exportedItems.Where(item => item.Name.Equals("CastingFailedForOtherReasons")));
    }

    [Fact(Skip = "Flaky test, bug: https://microsofthealth.visualstudio.com/Health/_boards/board/t/Medical%20Imaging/Stories/?workitem=78349")]
    public async Task GivenWorker_WhenExecuting_ThenPollIntervalShouldBeHonored()
    {
        var pollInterval = TimeSpan.FromMilliseconds(50);

        _dicomCastWorkerConfiguration.PollInterval = pollInterval;

        int invocationCount = 0;

        var stopwatch = new Stopwatch();

        _changeFeedProcessor.When(processor => processor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken))
            .Do(_ =>
            {
                if (invocationCount++ == 0)
                {
                    stopwatch.Start();
                }
                else
                {
                    stopwatch.Stop();

                    _cancellationTokenSource.Cancel();
                }
            });

        await _dicomCastWorker.ExecuteAsync(_cancellationToken);

        Assert.True(stopwatch.ElapsedMilliseconds >= pollInterval.TotalMilliseconds);
    }

    private void InitializeMetricExporter()
    {
        _exportedItems = new List<Metric>();
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("Microsoft.Health.DicomCast", "1.0")
            .AddInMemoryExporter(_exportedItems)
            .Build();
    }
}
