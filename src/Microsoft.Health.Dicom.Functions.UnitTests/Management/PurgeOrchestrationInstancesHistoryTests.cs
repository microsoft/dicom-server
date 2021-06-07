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

namespace Microsoft.Health.Dicom.Functions.UnitTests
{
    public class PurgeOrchestrationInstancesHistoryTests
    {
        private readonly PurgeOrchestrationInstancesHistoryConfiguration _purgeConfig;
        private readonly TimerInfo _timer;
        private readonly ILogger _logger;
        private readonly DateTime _definedNow;
        private readonly PurgeOrchestrationInstancesHistory _purgeOrchestrationInstancesHistory;
        private readonly IDurableOrchestrationClient _durableOrchestrationClientMock;

        public PurgeOrchestrationInstancesHistoryTests()
        {
            _purgeConfig = new PurgeOrchestrationInstancesHistoryConfiguration();
            _purgeConfig.RuntimeStatuses = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Completed };
            _timer = Substitute.For<TimerInfo>(default, default, default);
            _logger = Substitute.For<ILogger>();
            _definedNow = DateTime.UtcNow;
            _purgeOrchestrationInstancesHistory = new PurgeOrchestrationInstancesHistory(
                Options.Create(_purgeConfig),
                () => _definedNow);
            _durableOrchestrationClientMock = Substitute.For<IDurableOrchestrationClient>();
        }

        [Fact]
        public async Task GivenValidInput_PurgeCompletedDurableFunctionsHistory_ShouldNotPurgeAnythingOrchestrationsAsync()
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
        public async Task GivenValidInput_PurgeCompletedDurableFunctionsHistory_ShouldPurgeMultiPageListOfOrchestrationsAsync()
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            List<DurableOrchestrationStatus> durableOrchestrationStatuses = new List<DurableOrchestrationStatus>()
            {
                new DurableOrchestrationStatus{ InstanceId = "1" },
                new DurableOrchestrationStatus{ InstanceId = "2" },
                new DurableOrchestrationStatus{ InstanceId = "3" }
            };

            OrchestrationStatusQueryResult orchestrationStatusQueryResult = new OrchestrationStatusQueryResult();
            orchestrationStatusQueryResult.DurableOrchestrationState = durableOrchestrationStatuses;
            orchestrationStatusQueryResult.ContinuationToken = "fake_cancellation_token";

            var collector = new List<string>();

            _durableOrchestrationClientMock
                .ListInstancesAsync(
                    Arg.Is<OrchestrationStatusQueryCondition>(
                            givenCondition => IsCondition(givenCondition, null)),
                    Arg.Is<CancellationToken>(cancellationTokenSource.Token))
                // First page, should have 3 orchestrations, then change continuation token.
                .Returns(_ =>
                    {
                        orchestrationStatusQueryResult.ContinuationToken = null;
                        Assert.True(orchestrationStatusQueryResult.DurableOrchestrationState.SequenceEqual(durableOrchestrationStatuses));
                        return Task.FromResult(orchestrationStatusQueryResult);
                    });

            _durableOrchestrationClientMock
                .ListInstancesAsync(
                    Arg.Is<OrchestrationStatusQueryCondition>(
                            givenCondition => IsCondition(givenCondition, orchestrationStatusQueryResult.ContinuationToken)),
                    Arg.Is<CancellationToken>(cancellationTokenSource.Token))
                // Second page, should have no orchestrations.
                .Returns(_ =>
                    {
                        Assert.True(orchestrationStatusQueryResult.DurableOrchestrationState.SequenceEqual(null));
                        return Task.FromResult(new OrchestrationStatusQueryResult());
                    });

            _durableOrchestrationClientMock
                .When(x => x.PurgeInstanceHistoryAsync(Arg.Any<string>()))
                .Do(x => collector.Add(x.Arg<string>()));

            await _purgeOrchestrationInstancesHistory.Run(_timer, _durableOrchestrationClientMock, _logger, cancellationTokenSource.Token);

            await _durableOrchestrationClientMock
                .Received(1)
                .ListInstancesAsync(
                    Arg.Is<OrchestrationStatusQueryCondition>(x => IsCondition(x, null)),
                    Arg.Is<CancellationToken>(cancellationTokenSource.Token));

            var expected = durableOrchestrationStatuses.Select(i => i.InstanceId);

            await _durableOrchestrationClientMock
                .Received(3)
                .PurgeInstanceHistoryAsync(Arg.Is<string>(x => expected.Contains(x)));
            Assert.Equal(expected, collector);
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
