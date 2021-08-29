// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenNewOrchestrationWithWork_WhenReindexingInstances_ThenDivideAndReindexBatches()
        {
            var expectedInput = new ReindexInput { QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 } };
            var expectedTags = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, null)
            };

            const int batchSize = 5;
            _options.BatchSize = batchSize;
            _options.MaxParallelBatches = 3;

            // Arrange the input
            IDurableOrchestrationContext context = CreateContext();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);
            context
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    input: null)
                .Returns(49);
            context
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<ReindexBatch>())
                .Returns(Task.CompletedTask);

            // Invoke the orchestration
            await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

            // Assert behavior
            context
                .Received(1)
                .GetInput<ReindexInput>();
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    expectedInput.QueryTagKeys);
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    input: null);
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 45, 50)));
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 40, 45)));
            await context
                 .Received(1)
                 .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 35, 40)));
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate((int)(15 / 49D * 100), "01010101", "02020202", "04040404")));
            context
                .Received(1)
                .ContinueAsNew(
                    Arg.Is<ReindexInput>(x => GetReindexInputPredicate(expectedTags, 35, 50)(x)),
                    false);
        }

        [Fact]
        public async Task GivenExistingOrchestrationWithWork_WhenReindexingInstances_ThenDivideAndReindexBatches()
        {
            var expectedInput = new ReindexInput
            {
                QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 },
                Completed = WatermarkRange.Between(36, 42),
            };
            var expectedTags = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, null)
            };

            const int batchSize = 3;
            _options.BatchSize = batchSize;
            _options.MaxParallelBatches = 2;

            // Arrange the input
            IDurableOrchestrationContext context = CreateContext();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    input: null)
                .Returns(expectedTags);
            context
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<ReindexBatch>())
                .Returns(Task.CompletedTask);

            // Invoke the orchestration
            await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

            // Assert behavior
            context
                .Received(1)
                .GetInput<ReindexInput>();
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    input: null);
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 33, 36)));
            await context
                .Received(1)
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 30, 33)));
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate((int)(12 / 41D * 100), "01010101", "02020202", "04040404")));
            context
                .Received(1)
                .ContinueAsNew(
                    Arg.Is<ReindexInput>(x => GetReindexInputPredicate(expectedTags, 30, 42)(x)),
                    false);
        }

        [Fact]
        public async Task GivenNoInstances_WhenReindexingInstances_ThenComplete()
        {
            var expectedInput = new ReindexInput { QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 } };
            var expectedTags = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, null)
            };

            // Arrange the input
            IDurableOrchestrationContext context = CreateContext();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);
            context
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    input: null)
                .Returns(0);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))))
                .Returns(expectedTags.Select(x => x.Key).ToList());

            // Invoke the orchestration
            await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

            // Assert behavior
            context
                .Received(1)
                .GetInput<ReindexInput>();
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    expectedInput.QueryTagKeys);
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    input: null);
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))));
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate(100, "01010101", "02020202", "04040404")));
            context
                .DidNotReceiveWithAnyArgs()
                .ContinueAsNew(default, default);
        }

        [Fact]
        public async Task GivenNoRemainingInstances_WhenReindexingInstances_ThenComplete()
        {
            var expectedInput = new ReindexInput
            {
                QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 },
                Completed = WatermarkRange.Between(1, 1000),
            };
            var expectedTags = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, null),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, null)
            };

            // Arrange the input
            IDurableOrchestrationContext context = CreateContext();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    input: null)
                .Returns(expectedTags);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))))
                .Returns(expectedTags.Select(x => x.Key).ToList());

            // Invoke the orchestration
            await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

            // Assert behavior
            context
                .Received(1)
                .GetInput<ReindexInput>();
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    input: null);
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))));
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate(100, "01010101", "02020202", "04040404")));
            context
                .DidNotReceiveWithAnyArgs()
                .ContinueAsNew(default, default);
        }

        [Fact]
        public async Task GivenNoQueryTags_WhenReindexingInstances_ThenComplete()
        {
            var expectedInput = new ReindexInput { QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 } };
            var expectedTags = new List<ExtendedQueryTagStoreEntry>();

            // Arrange the input
            IDurableOrchestrationContext context = CreateContext();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);

            // Invoke the orchestration
            await _reindexDurableFunction.ReindexInstancesAsync(context, NullLogger.Instance);

            // Assert behavior
            context
                .Received(1)
                .GetInput<ReindexInput>();
            await context
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.AssignReindexingOperationAsync),
                    _options.ActivityRetryOptions,
                    expectedInput.QueryTagKeys);
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<long>(
                    nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate(100)));
            context
                .DidNotReceiveWithAnyArgs()
                .ContinueAsNew(default, default);
        }

        private static IDurableOrchestrationContext CreateContext(Guid? instanceId = null)
        {
            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.InstanceId.Returns(OperationId.ToString(instanceId ?? Guid.NewGuid()));
            return context;
        }

        private static Expression<Predicate<ReindexBatch>> GetReindexBatchPredicate(
            IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags,
            long start,
            long end)
        {
            return x => ReferenceEquals(x.QueryTags, queryTags)
                && x.WatermarkRange == WatermarkRange.Between(start, end);
        }

        private static Predicate<object> GetReindexInputPredicate(
            IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags,
            long start,
            long end)
        {
            return x => x is ReindexInput r
                && r.QueryTagKeys.SequenceEqual(queryTags.Select(y => y.Key))
                && r.Completed == WatermarkRange.Between(start, end);
        }

        private static Expression<Predicate<OperationCustomStatus>> GetCustomStatePredicate(int percentComplete, params string[] resourceIds)
        {
            return x => x.PercentComplete == percentComplete
                && (resourceIds.Length == 0 ? x.ResourceIds == null : x.ResourceIds.SequenceEqual(resourceIds));
        }
    }
}
