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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using Microsoft.Health.Operations;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.HealthChecks;

public class DurableTaskHealthCheckTests
{
    private static readonly Guid HealthCheckOperationId = Guid.NewGuid();

    private readonly IDurableClient _durableClient;
    private readonly DurableTaskHealthCheck _healthCheck;

    public DurableTaskHealthCheckTests()
    {
        IDurableClientFactory durableClientFactory = Substitute.For<IDurableClientFactory>();
        _durableClient = Substitute.For<IDurableClient>();
        durableClientFactory.CreateClient().Returns(_durableClient);

        IGuidFactory guidFactory = Substitute.For<IGuidFactory>();
        guidFactory.Create().Returns(HealthCheckOperationId);

        _healthCheck = new DurableTaskHealthCheck(durableClientFactory, guidFactory, NullLogger<DurableTaskHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenCannotConnectToTaskHub_ThenThrowException()
    {
        using var tokenSource = new CancellationTokenSource();

        _durableClient
            .GetStatusAsync(
                HealthCheckOperationId.ToString(OperationId.FormatSpecifier),
                showHistory: false,
                showHistoryOutput: false,
                showInput: false)
            .Throws<IOException>();

        await Assert.ThrowsAsync<IOException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));

        await _durableClient
            .Received(1)
            .GetStatusAsync(
                HealthCheckOperationId.ToString(OperationId.FormatSpecifier),
                showHistory: false,
                showHistoryOutput: false,
                showInput: false);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenCanConnectToTaskHub_ThenReturnHealthy()
    {
        DateTime now = DateTime.UtcNow;
        ClockResolver.UtcNowFunc = () => now;
        using var tokenSource = new CancellationTokenSource();

        _durableClient
            .GetStatusAsync(
                HealthCheckOperationId.ToString(OperationId.FormatSpecifier),
                showHistory: false,
                showHistoryOutput: false,
                showInput: false)
            .Returns(new DurableOrchestrationStatus());

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);

        await _durableClient
            .Received(1)
            .GetStatusAsync(
                HealthCheckOperationId.ToString(OperationId.FormatSpecifier),
                showHistory: false,
                showHistoryOutput: false,
                showInput: false);

        Assert.Equal(HealthStatus.Healthy, actual.Status);
    }
}
