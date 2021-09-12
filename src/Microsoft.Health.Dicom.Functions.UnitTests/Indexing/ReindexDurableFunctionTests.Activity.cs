// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenTagKeys_WhenAssigningReindexingOperation_ThenShouldPassArguments()
        {
            Guid operationId = Guid.NewGuid();
            var expectedInput = new List<int> { 1, 2, 3, 4, 5 };
            var expectedOutput = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryTagQueryStatus.Enabled)
            };

            // Arrange input
            IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
            context.InstanceId.Returns(OperationId.ToString(operationId));
            context.GetInput<IReadOnlyList<int>>().Returns(expectedInput);

            _extendedQueryTagStore
                .AssignReindexingOperationAsync(expectedInput, operationId, false, CancellationToken.None)
                .Returns(expectedOutput);

            // Call the activity
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _reindexDurableFunction.AssignReindexingOperationAsync(
                context,
                NullLogger.Instance);

            // Assert behavior
            Assert.Same(expectedOutput, actual);
            context.Received(1).GetInput<IReadOnlyList<int>>();
            await _extendedQueryTagStore
                .Received(1)
                .AssignReindexingOperationAsync(expectedInput, operationId, false, CancellationToken.None);
        }

        [Fact]
        public async Task GivenTagKeys_WhenGettingExtentendedQueryTags_ThenShouldPassArguments()
        {
            Guid operationId = Guid.NewGuid();
            var expectedOutput = new List<ExtendedQueryTagStoreEntry>
            {
                new ExtendedQueryTagStoreEntry(1, "01010101", "AS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryTagQueryStatus.Enabled)
            };

            // Arrange input
            IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
            context.InstanceId.Returns(OperationId.ToString(operationId));

            _extendedQueryTagStore
                .GetExtendedQueryTagsByOperationAsync(operationId, CancellationToken.None)
                .Returns(expectedOutput);

            // Call the activity
            IReadOnlyList<ExtendedQueryTagStoreEntry> actual = await _reindexDurableFunction.GetQueryTagsAsync(
                context,
                NullLogger.Instance);

            // Assert behavior
            Assert.Same(expectedOutput, actual);
            await _extendedQueryTagStore
                .Received(1)
                .GetExtendedQueryTagsByOperationAsync(operationId, CancellationToken.None);
        }

        [Fact]
        public async Task GivenNoWatermark_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethod()
        {
            IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(12345, 678910) };
            _instanceStore
                .GetInstanceBatchesAsync(_options.BatchSize, _options.MaxParallelBatches, IndexStatus.Created, null, CancellationToken.None)
                .Returns(expected);

            IReadOnlyList<WatermarkRange> actual = await _reindexDurableFunction.GetInstanceBatchesAsync(
                null,
                NullLogger.Instance);

            Assert.Same(expected, actual);
            await _instanceStore
                .Received(1)
                .GetInstanceBatchesAsync(_options.BatchSize, _options.MaxParallelBatches, IndexStatus.Created, null, CancellationToken.None);
        }

        [Fact]
        public async Task GivenWatermark_WhenGettingInstanceBatches_ThenShouldInvokeCorrectMethod()
        {
            IReadOnlyList<WatermarkRange> expected = new List<WatermarkRange> { new WatermarkRange(10, 1000) };
            _instanceStore
                .GetInstanceBatchesAsync(_options.BatchSize, _options.MaxParallelBatches, IndexStatus.Created, 12345L, CancellationToken.None)
                .Returns(expected);

            IReadOnlyList<WatermarkRange> actual = await _reindexDurableFunction.GetInstanceBatchesAsync(
                12345L,
                NullLogger.Instance);

            Assert.Same(expected, actual);
            await _instanceStore
                .Received(1)
                .GetInstanceBatchesAsync(_options.BatchSize, _options.MaxParallelBatches, IndexStatus.Created, 12345L, CancellationToken.None);
        }

        [Fact]
        public async Task GivenBatch_WhenReindexing_ThenShouldReindexEachInstance()
        {
            var batch = new ReindexBatch
            {
                QueryTags = new List<ExtendedQueryTagStoreEntry>
                {
                    new ExtendedQueryTagStoreEntry(1, "01", "DT", "foo", QueryTagLevel.Instance, ExtendedQueryTagStatus.Adding, QueryTagQueryStatus.Enabled),
                    new ExtendedQueryTagStoreEntry(2, "02", "DT", null, QueryTagLevel.Series, ExtendedQueryTagStatus.Adding, QueryTagQueryStatus.Enabled),
                    new ExtendedQueryTagStoreEntry(3, "03", "AS", "bar", QueryTagLevel.Study, ExtendedQueryTagStatus.Adding, QueryTagQueryStatus.Enabled),
                },
                WatermarkRange = new WatermarkRange(3, 10),
            };

            var expected = new List<VersionedInstanceIdentifier>
            {
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 3),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 4),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 5),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 6),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 7),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 8),
                new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 9),
            };

            // Arrange input
            _instanceStore
                .GetInstanceIdentifiersByWatermarkRangeAsync(batch.WatermarkRange, IndexStatus.Created, CancellationToken.None)
                .Returns(expected);

            foreach (VersionedInstanceIdentifier identifier in expected)
            {
                _instanceReindexer.ReindexInstanceAsync(batch.QueryTags, identifier).Returns(Task.CompletedTask);
            }

            // Call the activity
            await _reindexDurableFunction.ReindexBatchAsync(batch, NullLogger.Instance);

            // Assert behavior
            await _instanceStore
                .Received(1)
                .GetInstanceIdentifiersByWatermarkRangeAsync(batch.WatermarkRange, IndexStatus.Created, CancellationToken.None);

            foreach (VersionedInstanceIdentifier identifier in expected)
            {
                await _instanceReindexer.Received(1).ReindexInstanceAsync(batch.QueryTags, identifier);
            }
        }

        [Fact]
        public async Task GivenTagKeys_WhenCompletingReindexing_ThenShouldPassArguments()
        {
            string operationId = Guid.NewGuid().ToString();
            var expectedInput = new List<int> { 1, 2, 3, 4, 5 };
            var expectedOutput = new List<int> { 1, 2, 4, 5 };

            // Arrange input
            IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
            context.InstanceId.Returns(operationId);
            context.GetInput<IReadOnlyList<int>>().Returns(expectedInput);

            _extendedQueryTagStore
                .CompleteReindexingAsync(expectedInput, CancellationToken.None)
                .Returns(expectedOutput);

            // Call the activity
            IReadOnlyList<int> actual = await _reindexDurableFunction.CompleteReindexingAsync(
                context,
                NullLogger.Instance);

            // Assert behavior
            Assert.Same(expectedOutput, actual);
            context.Received(1).GetInput<IReadOnlyList<int>>();
            await _extendedQueryTagStore
                .Received(1)
                .CompleteReindexingAsync(expectedInput, CancellationToken.None);
        }
    }
}
