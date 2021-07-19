// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Indexing.Models;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenManyInstances_WhenCallSubReindexTagsAsync_ShouldCallStartNewOrchestration()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntries = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { WatermarkRange = new WatermarkRange(1, 10), OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntries };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);

            await _reindexDurableFunction.SubReindexTagsAsync(context, NullLogger.Instance);

            // Verify ReindexInstanceActivityAsync is called
            for (int i = 0; i < _reindexConfig.MaxParallelBatches; i++)
            {
                await context.Received().
                     CallActivityAsync(nameof(ReindexDurableFunction.ReindexBatchAsync),
                         Arg.Is<ReindexBatch>(x => x.WatermarkRange.Start == reindexOperation.WatermarkRange.End - _reindexConfig.BatchSize * i - 1
                         && x.WatermarkRange.End == reindexOperation.WatermarkRange.End - _reindexConfig.BatchSize * i));
            }


            // Verify  StartNewOrchestration
            context.Received().StartNewOrchestration(nameof(ReindexDurableFunction.SubReindexTagsAsync),
                Arg.Is<ReindexOperation>(
                    x => x.WatermarkRange.Start == reindexOperation.WatermarkRange.Start
                    && x.WatermarkRange.End == reindexOperation.WatermarkRange.End - _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches
                    && x.StoreEntries == storeEntries));
        }

        [Fact]
        public async Task GivenFewInstances_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntries = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { WatermarkRange = new WatermarkRange(1, 3), OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntries };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);

            await _reindexDurableFunction.SubReindexTagsAsync(context, NullLogger.Instance);

            // Verify ReindexInstancesAsync is called
            for (int i = 0; i < _reindexConfig.MaxParallelBatches; i++)
            {
                await context.Received().
                     CallActivityAsync(nameof(ReindexDurableFunction.ReindexBatchAsync),
                         Arg.Is<ReindexBatch>(x => x.WatermarkRange.Start == Math.Max(reindexOperation.WatermarkRange.Start, reindexOperation.WatermarkRange.End - _reindexConfig.BatchSize * i - 1)
                         && x.WatermarkRange.End == reindexOperation.WatermarkRange.End - _reindexConfig.BatchSize * i));
            }

            // Verify  CompleteReindexAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexingAsync),
                    Arg.Is<IReadOnlyCollection<int>>(x => x.SequenceEqual(reindexOperation.StoreEntries.Select(x => x.Key))));

            // Verify StartNewOrchestration is not called
            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.SubReindexTagsAsync), Arg.Any<ReindexOperation>());
        }

        [Fact]
        public async Task GivenNoInstances_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { WatermarkRange = new WatermarkRange(-1, -1), OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);

            await _reindexDurableFunction.SubReindexTagsAsync(context, NullLogger.Instance);

            await context.Received().
                  CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexingAsync),
                  Arg.Is<IReadOnlyCollection<int>>(x => x.SequenceEqual(reindexOperation.StoreEntries.Select(y => y.Key))));

            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.SubReindexTagsAsync), Arg.Any<ReindexOperation>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.ReindexBatchAsync), Arg.Any<ReindexBatch>());
        }
    }
}
