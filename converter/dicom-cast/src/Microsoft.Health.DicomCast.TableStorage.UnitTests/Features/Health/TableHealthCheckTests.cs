// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.DicomCast.TableStorage.Features.Health;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.DicomCast.TableStorage.UnitTests.Features.Health;

public class TableHealthCheckTests
{
    private readonly ITableClientTestProvider _testProvider = Substitute.For<ITableClientTestProvider>();

    private readonly TableHealthCheck _healthCheck;

    public TableHealthCheckTests()
    {
        _healthCheck = new TableHealthCheck(_testProvider, NullLogger<TableHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenTableDataStoreIsAvailable_WhenTableIsChecked_ThenHealthyStateShouldBeReturned()
    {
        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task GivenTableDataStoreIsNotAvailable_WhenHealthIsChecked_ThenUnhealthyStateShouldBeReturned()
    {
        _testProvider.PerformTestAsync(default).ThrowsForAnyArgs<HttpRequestException>();
        await Assert.ThrowsAsync<HttpRequestException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext()));
    }
}
