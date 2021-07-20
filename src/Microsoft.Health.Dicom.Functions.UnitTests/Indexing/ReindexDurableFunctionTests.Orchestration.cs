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
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding)
            };

            const int batchSize = 5;
            _options.BatchSize = batchSize;
            _options.MaxParallelBatches = 3;

            // Arrange the input
            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);
            context
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null)
                .Returns(49);
            context
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
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
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys);
            await context
                .Received(1)
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null);
            await context
                .Received(1)
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 45, 50)));
            await context
                .Received(1)
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 40, 45)));
            await context
                 .Received(1)
                 .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 35, 40)));
            await context
                .DidNotReceive()
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    Arg.Any<IReadOnlyList<int>>());
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
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding)
            };

            const int batchSize = 3;
            _options.BatchSize = batchSize;
            _options.MaxParallelBatches = 2;

            // Arrange the input
            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);
            context
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
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
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys);
            await context
                .DidNotReceive()
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null);
            await context
                .Received(1)
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 33, 36)));
            await context
                .Received(1)
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Is(GetReindexBatchPredicate(expectedTags, 30, 33)));
            await context
                .DidNotReceive()
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    Arg.Any<IReadOnlyList<int>>());
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
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding)
            };

            // Arrange the input
            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);
            context
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null)
                .Returns(0);
            context
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
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
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys);
            await context
                .Received(1)
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null);
            await context
                .DidNotReceive()
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Any<ReindexInput>());
            await context
                .Received(1)
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))));
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
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(2, "02020202", "IS", "foo", QueryTagLevel.Series, ExtendedQueryTagStatus.Adding),
                new ExtendedQueryTagStoreEntry(4, "04040404", "SH", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Adding)
            };

            // Arrange the input
            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys)
                .Returns(expectedTags);
            context
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
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
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys);
            await context
                .DidNotReceive()
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null);
            await context
                .DidNotReceive()
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Any<ReindexInput>());
            await context
                .Received(1)
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    Arg.Is<IReadOnlyList<int>>(x => x.SequenceEqual(expectedTags.Select(x => x.Key))));
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
            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context
                .GetInput<ReindexInput>()
                .Returns(expectedInput);
            context
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
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
                .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                    nameof(ReindexDurableFunction.GetQueryTagsAsync),
                    expectedInput.QueryTagKeys);
            await context
                .DidNotReceive()
                .CallActivityAsync<long>(nameof(ReindexDurableFunction.GetMaxInstanceWatermarkAsync), input: null);
            await context
                .DidNotReceive()
                .CallActivityAsync(
                    nameof(ReindexDurableFunction.ReindexBatchAsync),
                    Arg.Any<ReindexInput>());
            await context
                .DidNotReceive()
                .CallActivityAsync<IReadOnlyList<int>>(
                    nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    Arg.Any<IReadOnlyList<int>>());
            context
                .DidNotReceiveWithAnyArgs()
                .ContinueAsNew(default, default);
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
    }
}
