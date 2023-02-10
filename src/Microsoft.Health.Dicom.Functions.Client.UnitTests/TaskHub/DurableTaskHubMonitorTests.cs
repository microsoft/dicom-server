// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class DurableTaskHubMonitorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DurableTaskHubMonitorOptions _options;
    private readonly ITaskHubClient _client = Substitute.For<ITaskHubClient>();
    private readonly DurableTaskHubMonitor _monitor;

    public DurableTaskHubMonitorTests()
    {
        _options = new DurableTaskHubMonitorOptions { Enabled = true, PollingInterval = TimeSpan.Zero };
        _serviceProvider = new ServiceCollection().AddScoped(sp => _client).BuildServiceProvider();
        _monitor = new DurableTaskHubMonitor(_serviceProvider, Options.Create(_options), NullLogger<DurableTaskHubMonitor>.Instance);
    }

    [Fact]
    public async Task GivenDisabledMonitor_WhenCheckingReadiness_ThenSkip()
    {
        using var tokenSource = new CancellationTokenSource();

        _options.Enabled = false;
        _client.GetTaskHubAsync(tokenSource.Token).Returns((ITaskHub)null);

        await _monitor.StartAsync(tokenSource.Token);

        await _client.Received(0).GetTaskHubAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenInitializingTaskHub_WhenCheckingReadiness_ThenWaitForCreation()
    {
        using var tokenSource = new CancellationTokenSource();
        ITaskHub taskHub = Substitute.For<ITaskHub>();

        _client
            .GetTaskHubAsync(tokenSource.Token)
            .Returns(
                c => ValueTask.FromException<ITaskHub>(new RequestFailedException("Unexpected")),
                c => ValueTask.FromResult<ITaskHub>(null),
                c => ValueTask.FromResult(taskHub),
                c => ValueTask.FromResult(taskHub));
        taskHub.IsReadyAsync(tokenSource.Token).Returns(false, true);

        await _monitor.StartAsync(tokenSource.Token);

        await _client.Received(4).GetTaskHubAsync(tokenSource.Token);
        await taskHub.Received(2).IsReadyAsync(tokenSource.Token);
    }
}
