// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.HealthChecks;

public class DurableTaskHealthCheckTests
{
    private readonly ITaskHubClient _client = Substitute.For<ITaskHubClient>();
    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache = new ValueCache<CustomerKeyHealth>();
    private readonly DurableTaskHealthCheck _healthCheck;

    public DurableTaskHealthCheckTests()
    {
        _customerKeyHealthCache.Set(new CustomerKeyHealth { IsHealthy = true });
        _healthCheck = new DurableTaskHealthCheck(_client, _customerKeyHealthCache, NullLogger<DurableTaskHealthCheck>.Instance);
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

    [Fact]
    public async Task GivenPrerequisiteIsNotHealthy_WhenCheckingHealth_ThenReturnDegraded()
    {
        using var tokenSource = new CancellationTokenSource();
        _customerKeyHealthCache.Set(new CustomerKeyHealth
        {
            IsHealthy = false,
            Reason = HealthStatusReason.CustomerManagedKeyAccessLost,
        });

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Degraded, actual.Status);
    }
}
