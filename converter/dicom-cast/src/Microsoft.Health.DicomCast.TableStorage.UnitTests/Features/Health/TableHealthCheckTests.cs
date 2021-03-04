// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Health;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.DicomCast.TableStorage.UnitTests.Features.Health
{
    public class TableHealthCheckTests
    {
        private readonly CloudTableClient _client = Substitute.For<CloudTableClient>(new Uri("https://www.microsoft.com/"), new StorageCredentials(), new TableClientConfiguration());
        private readonly ITableClientTestProvider _testProvider = Substitute.For<ITableClientTestProvider>();
        private readonly TableDataStoreConfiguration _configuration = new TableDataStoreConfiguration { };

        private readonly TableHealthCheck _healthCheck;

        public TableHealthCheckTests()
        {
            _healthCheck = new TableHealthCheck(_client, _configuration, _testProvider, NullLogger<TableHealthCheck>.Instance);
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
            _testProvider.PerformTestAsync(default, default).ThrowsForAnyArgs<HttpRequestException>();
            HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }
    }
}
