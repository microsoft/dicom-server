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
            const int batchSize = 5;
            _options.BatchSize = batchSize;
            _options.MaxParallelBatches = 3;

            IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(50);
            int expectedPercentage = (int)((double)(50 - expectedBatches[^1].Start) / 49 * 100);
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    input: null)
                .Returns(expectedBatches);
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    input: null);

            foreach (WatermarkRange batch in expectedBatches)
            {
                await context
                    .Received(1)
                    .CallActivityWithRetryAsync(
                        nameof(ReindexDurableFunction.ReindexBatchAsync),
                        _options.ActivityRetryOptions,
                        Arg.Is(GetReindexBatchPredicate(expectedTags, batch)));
            }

            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate(expectedPercentage, "01010101", "02020202", "04040404")));
            context
                .Received(1)
                .ContinueAsNew(
                    Arg.Is<ReindexInput>(x => GetReindexInputPredicate(expectedTags, expectedBatches, 50)(x)),
                    false);
        }

        [Fact]
        public async Task GivenExistingOrchestrationWithWork_WhenReindexingInstances_ThenDivideAndReindexBatches()
        {
            const int batchSize = 3;
            _options.BatchSize = batchSize;
            _options.MaxParallelBatches = 2;

            IReadOnlyList<WatermarkRange> expectedBatches = CreateBatches(35);
            int expectedPercentage = (int)((double)(42 - expectedBatches[^1].Start) / 41 * 100);
            var expectedInput = new ReindexInput
            {
                QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 },
                Completed = new WatermarkRange(36, 42),
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    35L)
                .Returns(expectedBatches);
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
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    35L);

            foreach (WatermarkRange batch in expectedBatches)
            {
                await context
                    .Received(1)
                    .CallActivityWithRetryAsync(
                        nameof(ReindexDurableFunction.ReindexBatchAsync),
                        _options.ActivityRetryOptions,
                        Arg.Is(GetReindexBatchPredicate(expectedTags, batch)));
            }

            await context
                .DidNotReceive()
                .CallActivityWithRetryAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    _options.ActivityRetryOptions,
                    Arg.Any<object>());
            context
                .Received(1)
                .SetCustomStatus(Arg.Is(GetCustomStatePredicate(expectedPercentage, "01010101", "02020202", "04040404")));
            context
                .Received(1)
                .ContinueAsNew(
                    Arg.Is<ReindexInput>(x => GetReindexInputPredicate(expectedTags, expectedBatches, 42)(x)),
                    false);
        }

        [Fact]
        public async Task GivenNoInstances_WhenReindexingInstances_ThenComplete()
        {
            var expectedBatches = new List<WatermarkRange>();
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    input: null)
                .Returns(expectedBatches);
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
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
            var expectedBatches = new List<WatermarkRange>();
            var expectedInput = new ReindexInput
            {
                QueryTagKeys = new List<int> { 1, 2, 3, 4, 5 },
                Completed = new WatermarkRange(5, 1000),
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    4L)
                .Returns(expectedBatches);
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
                .Received(1)
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
                    _options.ActivityRetryOptions,
                    4L);
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
                .CallActivityWithRetryAsync<IReadOnlyList<WatermarkRange>>(
                    nameof(ReindexDurableFunction.GetInstanceBatchesAsync),
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

        private IReadOnlyList<WatermarkRange> CreateBatches(long end)
        {
            var batches = new List<WatermarkRange>();

            long current = end;
            for (int i = 0; i < _options.MaxParallelBatches && current > 0; i++)
            {
                batches.Add(new WatermarkRange(Math.Max(1, current - _options.BatchSize + 1), current));
                current -= _options.BatchSize;
            }

            return batches;
        }

        private static Expression<Predicate<ReindexBatch>> GetReindexBatchPredicate(
            IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags,
            WatermarkRange expected)
        {
            return x => ReferenceEquals(x.QueryTags, queryTags)
                && x.WatermarkRange == expected;
        }

        private static Predicate<object> GetReindexInputPredicate(
            IReadOnlyList<ExtendedQueryTagStoreEntry> queryTags,
            IReadOnlyList<WatermarkRange> expectedBatches,
            long end)
        {
            return x => x is ReindexInput r
                && r.QueryTagKeys.SequenceEqual(queryTags.Select(y => y.Key))
                && r.Completed == new WatermarkRange(expectedBatches[expectedBatches.Count - 1].Start, end);
        }

        private static Expression<Predicate<OperationCustomStatus>> GetCustomStatePredicate(int percentComplete, params string[] resourceIds)
        {
            return x => x.PercentComplete == percentComplete
                && (resourceIds.Length == 0 ? x.ResourceIds == null : x.ResourceIds.SequenceEqual(resourceIds));
        }
    }
}
