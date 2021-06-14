// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Operations.Functions.Indexing;
using Microsoft.Health.Dicom.Operations.Functions.Indexing.Models;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        [Fact]
        public async Task GivenValidTags_WhenAddExtendedQueryTagsOrchestrationAsync_ShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            IReadOnlyList<AddExtendedQueryTagEntry> tagsEntries = new[] { entry };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<IReadOnlyList<AddExtendedQueryTagEntry>>().Returns(tagsEntries);
            await _reindexDurableFunction.AddAndReindexTagsAsync(context, NullLogger.Instance);
            await context.Received()
                 .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                 nameof(ReindexDurableFunction.AddTagsAsync),
                  tagsEntries);

            await context.Received()
                .CallActivityAsync<ReindexOperation>(
                nameof(ReindexDurableFunction.PrepareReindexingTagsAsync),
                 Arg.Any<PrepareReindexingTagsInput>());

            await context.Received()
                 .CallSubOrchestratorAsync(
                nameof(ReindexDurableFunction.ReindexTagsAsync),
                Arg.Any<ReindexOperation>());
        }

        [Fact]
        public async Task GivenInvalidTags_WhenAddExtendedQueryTagsOrchestrationAsync_ShouldFailAtBegining()
        {
            IReadOnlyList<AddExtendedQueryTagEntry> tagsEntries = new AddExtendedQueryTagEntry[0];

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<IReadOnlyList<AddExtendedQueryTagEntry>>().Returns(tagsEntries);
            context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AddTagsAsync),
                 tagsEntries)
                .Returns<IReadOnlyList<ExtendedQueryTagStoreEntry>>(x => { throw new FunctionFailedException(""); });

            await Assert.ThrowsAsync<FunctionFailedException>(() => _reindexDurableFunction.AddAndReindexTagsAsync(context, NullLogger.Instance));
            await context.Received()
                 .CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(
                 nameof(ReindexDurableFunction.AddTagsAsync),
                  tagsEntries);

            await context.DidNotReceive()
                .CallActivityAsync<ReindexOperation>(
                nameof(ReindexDurableFunction.PrepareReindexingTagsAsync),
                 Arg.Any<PrepareReindexingTagsInput>());

            await context.DidNotReceive()
                 .CallSubOrchestratorAsync(
                nameof(ReindexDurableFunction.ReindexTagsAsync),
                Arg.Any<ReindexOperation>());
        }


        [Fact]
        public async Task GivenManyInstances_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCallStartNewOrchestration()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { StartWatermark = 1, EndWatermark = 10, OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);

            context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(ReindexDurableFunction.GetProcessingTagsAsync), reindexOperation.OperationId)
                .Returns(storeEntires);

            await _reindexDurableFunction.ReindexTagsAsync(context, NullLogger.Instance);

            // Verify ReindexInstanceActivityAsync is called
            for (int i = 0; i < _reindexConfig.MaxParallelBatches; i++)
            {
                await context.Received().
                     CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstancesAsync),
                         Arg.Is<ReindexInstanceInput>(x => x.WatermarkRange.Start == reindexOperation.EndWatermark - _reindexConfig.BatchSize * i - 1
                         && x.WatermarkRange.End == reindexOperation.EndWatermark - _reindexConfig.BatchSize * i));
            }

            // Verify  UpdateReindexProgressActivityAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressAsync),
                    Arg.Is<UpdateReindexProgressInput>(x => x.EndWatermark == reindexOperation.EndWatermark - _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches));


            // Verify  StartNewOrchestration
            context.Received().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexTagsAsync),
                Arg.Is<ReindexOperation>(
                    x => x.StartWatermark == reindexOperation.StartWatermark
                    && x.EndWatermark == reindexOperation.EndWatermark - _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches
                    && x.StoreEntries == storeEntires));
        }

        [Fact]
        public async Task GivenFewInstances_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { StartWatermark = 1, EndWatermark = 3, OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);

            context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(ReindexDurableFunction.GetProcessingTagsAsync), reindexOperation.OperationId)
                .Returns(storeEntires);

            await _reindexDurableFunction.ReindexTagsAsync(context, NullLogger.Instance);

            // Verify ReindexInstancesAsync is called
            for (int i = 0; i < _reindexConfig.MaxParallelBatches; i++)
            {
                await context.Received().
                     CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstancesAsync),
                         Arg.Is<ReindexInstanceInput>(x => x.WatermarkRange.Start == Math.Max(reindexOperation.StartWatermark, reindexOperation.EndWatermark - _reindexConfig.BatchSize * i - 1)
                         && x.WatermarkRange.End == reindexOperation.EndWatermark - _reindexConfig.BatchSize * i));
            }

            // Verify  UpdateReindexProgressAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressAsync),
                    Arg.Is<UpdateReindexProgressInput>(x =>
                    x.EndWatermark == Math.Max(reindexOperation.StartWatermark - 1, reindexOperation.EndWatermark - _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches)));

            // Verify  CompleteReindexAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexingTagsAsync), reindexOperation.OperationId);

            // Verify StartNewOrchestration is not called
            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexTagsAsync), Arg.Any<ReindexOperation>());
        }

        [Fact]
        public async Task GivenNoInstances_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { StartWatermark = -1, EndWatermark = -1, OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);
            context.CallActivityAsync<IReadOnlyList<ExtendedQueryTagStoreEntry>>(nameof(ReindexDurableFunction.GetProcessingTagsAsync), reindexOperation.OperationId)
                .Returns(new ExtendedQueryTagStoreEntry[0]);

            await _reindexDurableFunction.ReindexTagsAsync(context, NullLogger.Instance);

            await context.Received().
                  CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexingTagsAsync), reindexOperation.OperationId);

            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexTagsAsync), Arg.Any<ReindexOperation>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstancesAsync), Arg.Any<ReindexInstanceInput>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressAsync), Arg.Any<UpdateReindexProgressInput>());

        }

        [Fact]
        public async Task GivenNoProcessingTags_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { StartWatermark = 1, EndWatermark = 10, OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);
            await _reindexDurableFunction.ReindexTagsAsync(context, NullLogger.Instance);

            await context.Received().
                  CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexingTagsAsync), reindexOperation.OperationId);

            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexTagsAsync), Arg.Any<ReindexOperation>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.GetProcessingTagsAsync), Arg.Any<string>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstancesAsync), Arg.Any<ReindexInstanceInput>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressAsync), Arg.Any<UpdateReindexProgressInput>());
        }
    }
}
