// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Health
{
    public class BlobHealthCheckTests
    {
        private readonly CloudBlobClient _client = Substitute.For<CloudBlobClient>(new Uri("https://www.microsoft.com/"), null);
        private readonly IBlobClientTestProvider _testProvider = Substitute.For<IBlobClientTestProvider>();
        private readonly BlobDataStoreConfiguration _configuration = new BlobDataStoreConfiguration { };
        private readonly BlobContainerConfiguration _containerConfiguration = new BlobContainerConfiguration { ContainerName = "mycont" };

        private readonly TestBlobHealthCheck _healthCheck;

        public BlobHealthCheckTests()
        {
            IOptionsSnapshot<BlobContainerConfiguration> optionsSnapshot = Substitute.For<IOptionsSnapshot<BlobContainerConfiguration>>();
            optionsSnapshot.Get(TestBlobHealthCheck.TestBlobHealthCheckName).Returns(_containerConfiguration);

            _healthCheck = new TestBlobHealthCheck(
                _client,
                _configuration,
                optionsSnapshot,
                _testProvider,
                NullLogger<TestBlobHealthCheck>.Instance);
        }

        [Fact]
        public async Task GivenBlobDataStoreIsAvailable_WhenHealthIsChecked_ThenHealthyStateShouldBeReturned()
        {
            HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task GivenBlobDataStoreIsNotAvailable_WhenHealthIsChecked_ThenUnhealthyStateShouldBeReturned()
        {
            _testProvider.PerformTestAsync(default, default, _containerConfiguration).ThrowsForAnyArgs<HttpRequestException>();
            HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }
    }
}
