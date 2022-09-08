// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Core.Internal;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.HealthChecks;

public class DurableTaskHealthCheckTests
{
    private readonly IDurableClient _durableClient;
    private readonly DurableTaskHealthCheck _healthCheck;

    public DurableTaskHealthCheckTests()
    {
        IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
        _durableClient = Substitute.For<IDurableClient>();
        durableClientFactory.CreateClient().Returns(_durableClient);
        _healthCheck = new DurableTaskHealthCheck(durableClientFactory, NullLogger<DurableTaskHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenStorageIsUnavailable_ThenThrowException()
    {
        DateTime now = DateTime.UtcNow;
        ClockResolver.UtcNowFunc = () => now;
        using var tokenSource = new CancellationTokenSource();

        _durableClient
            .ListInstancesAsync(
                Arg.Is<OrchestrationStatusQueryCondition>(x => x.CreatedTimeFrom == now && x.CreatedTimeTo == now.AddMinutes(1) && x.PageSize == 1),
                tokenSource.Token)
            .Throws<IOException>();

        await Assert.ThrowsAsync<IOException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));

        await _durableClient
            .Received(1)
            .ListInstancesAsync(
                Arg.Is<OrchestrationStatusQueryCondition>(x => x.CreatedTimeFrom == now && x.CreatedTimeTo == now.AddMinutes(1) && x.PageSize == 1),
                tokenSource.Token);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenStorageIsAvailable_ThenReturnHealthy()
    {
        DateTime now = DateTime.UtcNow;
        ClockResolver.UtcNowFunc = () => now;
        using var tokenSource = new CancellationTokenSource();

        _durableClient
            .ListInstancesAsync(
                Arg.Is<OrchestrationStatusQueryCondition>(x => x.CreatedTimeFrom == now && x.CreatedTimeTo == now.AddMinutes(1) && x.PageSize == 1),
                tokenSource.Token)
            .Returns(new OrchestrationStatusQueryResult { DurableOrchestrationState = new DurableOrchestrationStatus[] { new DurableOrchestrationStatus() } });

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);

        await _durableClient
            .Received(1)
            .ListInstancesAsync(
                Arg.Is<OrchestrationStatusQueryCondition>(x => x.CreatedTimeFrom == now && x.CreatedTimeTo == now.AddMinutes(1) && x.PageSize == 1),
                tokenSource.Token);

        Assert.Equal(HealthStatus.Healthy, actual.Status);
    }
}
