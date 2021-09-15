// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Management
{
    public class PurgeOrchestrationInstanceHistoryTests
    {
        private readonly PurgeHistoryOptions _purgeConfig;
        private readonly TimerInfo _timer;
        private readonly ILogger _logger;
        private readonly DateTime _definedNow;
        private readonly PurgeOrchestrationInstanceHistory _purgeOrchestrationInstancesHistory;
        private readonly IDurableOrchestrationClient _durableOrchestrationClientMock;

        public PurgeOrchestrationInstanceHistoryTests()
        {
            _purgeConfig = new PurgeHistoryOptions
            {
                RuntimeStatuses = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Completed }
            };
            _timer = Substitute.For<TimerInfo>(default, default, default);
            _logger = Substitute.For<ILogger>();
            _definedNow = DateTime.UtcNow;
            _purgeOrchestrationInstancesHistory = new PurgeOrchestrationInstanceHistory(
                Options.Create(_purgeConfig),
                () => _definedNow);
            _durableOrchestrationClientMock = Substitute.For<IDurableOrchestrationClient>();
        }

        [Fact]
        public async Task GivenNoOrchestrationInstances_WhenPurgeCompletedDurableFunctionsHistory_ThenNoOrchestrationsPurgedAsync()
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            _durableOrchestrationClientMock
                .ListInstancesAsync(
                    Arg.Is<OrchestrationStatusQueryCondition>(givenCondition => IsCondition(givenCondition, null)),
                    Arg.Is<CancellationToken>(cancellationTokenSource.Token))
                .Returns(Task.FromResult(new OrchestrationStatusQueryResult { DurableOrchestrationState = Array.Empty<DurableOrchestrationStatus>() }));

            await _purgeOrchestrationInstancesHistory.Run(_timer, _durableOrchestrationClientMock, _logger, cancellationTokenSource.Token);

            await _durableOrchestrationClientMock
                .DidNotReceiveWithAnyArgs()
                .PurgeInstanceHistoryAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task GivenContinuationToken_WhenPurgingHistory_ThenPurgeMultiplePagesAsync()
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var expected = new List<string> { "1", "2", "3", "4", "5", "6" };
            var actual = new List<string>();

            _durableOrchestrationClientMock
                .ListInstancesAsync(
                    Arg.Is<OrchestrationStatusQueryCondition>(
                            givenCondition => IsCondition(givenCondition, null)),
                    Arg.Is(cancellationTokenSource.Token))
                // First page, should have 3 orchestrations, then change continuation token.
                .Returns(Task.FromResult(
                    new OrchestrationStatusQueryResult
                    {
                        DurableOrchestrationState = expected
                            .Take(4)
                            .Select(x => new DurableOrchestrationStatus { InstanceId = x }),
                        ContinuationToken = "fake_cancellation_token"
                    }));

            _durableOrchestrationClientMock
                .ListInstancesAsync(
                    Arg.Is<OrchestrationStatusQueryCondition>(
                            givenCondition => IsCondition(givenCondition, "fake_cancellation_token")),
                    Arg.Is(cancellationTokenSource.Token))
                // Second page, should have no orchestrations.
                .Returns(Task.FromResult(
                    new OrchestrationStatusQueryResult
                    {
                        DurableOrchestrationState = expected
                            .Skip(4)
                            .Select(x => new DurableOrchestrationStatus { InstanceId = x }),
                        ContinuationToken = null
                    }));

            _durableOrchestrationClientMock
                .When(x => x.PurgeInstanceHistoryAsync(Arg.Any<string>()))
                .Do(x => actual.Add(x.Arg<string>()));

            await _purgeOrchestrationInstancesHistory.Run(_timer, _durableOrchestrationClientMock, _logger, cancellationTokenSource.Token);

            await _durableOrchestrationClientMock
                .ReceivedWithAnyArgs(2)
                .ListInstancesAsync(default, default);

            await _durableOrchestrationClientMock
                .Received(expected.Count)
                .PurgeInstanceHistoryAsync(Arg.Is<string>(x => expected.Contains(x)));
            Assert.Equal(expected, actual);
        }

        private bool IsCondition(OrchestrationStatusQueryCondition givenCondition, string continuationToken)
        {
            return givenCondition.RuntimeStatus.SequenceEqual(_purgeConfig.RuntimeStatuses)
                && givenCondition.CreatedTimeFrom == DateTime.MinValue
                && givenCondition.CreatedTimeTo == _definedNow.AddDays(-_purgeConfig.MinimumAgeDays)
                && givenCondition.ContinuationToken == continuationToken;
        }
    }
}
