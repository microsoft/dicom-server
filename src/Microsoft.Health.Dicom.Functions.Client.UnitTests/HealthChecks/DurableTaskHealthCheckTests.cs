// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.HealthChecks;

public class DurableTaskHealthCheckTests
{
    private readonly ITaskHubClient _client = Substitute.For<ITaskHubClient>();
    private readonly DurableTaskHealthCheck _healthCheck;

    public DurableTaskHealthCheckTests()
    {
        _healthCheck = new DurableTaskHealthCheck(_client, NullLogger<DurableTaskHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenMissingTaskHub_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        _client.GetTaskHubAsync(tokenSource.Token).Returns((ITaskHub)null);

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, actual.Status);

        await _client.Received(1).GetTaskHubAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenUnhealthyTaskHub_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();
        ITaskHub taskHub = Substitute.For<ITaskHub>();

        _client.GetTaskHubAsync(tokenSource.Token).Returns(taskHub);
        taskHub.IsHealthyAsync(tokenSource.Token).Returns(false);

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, actual.Status);

        await _client.Received(1).GetTaskHubAsync(tokenSource.Token);
        await taskHub.Received(1).IsHealthyAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableHealthCheck_WhenCheckingHealth_ThenReturnHealthy()
    {
        using var tokenSource = new CancellationTokenSource();
        ITaskHub taskHub = Substitute.For<ITaskHub>();

        _client.GetTaskHubAsync(tokenSource.Token).Returns(taskHub);
        taskHub.IsHealthyAsync(tokenSource.Token).Returns(true);

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, actual.Status);

        await _client.Received(1).GetTaskHubAsync(tokenSource.Token);
        await taskHub.Received(1).IsHealthyAsync(tokenSource.Token);
    }
}
