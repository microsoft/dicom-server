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
        public async Task GivenValidTags_WhenAddExtendedQueryTagsOrchestrationAsync_ShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            IEnumerable<AddExtendedQueryTagEntry> tagsEntries = new[] { entry };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<IEnumerable<AddExtendedQueryTagEntry>>().Returns(tagsEntries);
            await _reindexDurableFunction.AddExtendedQueryTagsOrchestrationAsync(context, NullLogger.Instance);
            await context.Received()
                 .CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(
                 nameof(ReindexDurableFunction.AddExtendedQueryTagsActivityAsync),
                  tagsEntries);

            await context.Received()
                .CallActivityAsync<ReindexOperation>(
                nameof(ReindexDurableFunction.StartReindexActivityAsync),
                 Arg.Any<StartReindexActivityInput>());

            await context.Received()
                 .CallSubOrchestratorAsync(
                nameof(ReindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync),
                Arg.Any<ReindexOperation>());
        }

        [Fact]
        public async Task GivenInvalidTags_WhenAddExtendedQueryTagsOrchestrationAsync_ShouldFailAtBegining()
        {
            IEnumerable<AddExtendedQueryTagEntry> tagsEntries = new AddExtendedQueryTagEntry[0];

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<IEnumerable<AddExtendedQueryTagEntry>>().Returns(tagsEntries);
            context.CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(
                nameof(ReindexDurableFunction.AddExtendedQueryTagsActivityAsync),
                 tagsEntries)
                .Returns<IEnumerable<ExtendedQueryTagStoreEntry>>(x => { throw new FunctionFailedException(""); });

            await Assert.ThrowsAsync<FunctionFailedException>(() => _reindexDurableFunction.AddExtendedQueryTagsOrchestrationAsync(context, NullLogger.Instance));
            await context.Received()
                 .CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(
                 nameof(ReindexDurableFunction.AddExtendedQueryTagsActivityAsync),
                  tagsEntries);

            await context.DidNotReceive()
                .CallActivityAsync<ReindexOperation>(
                nameof(ReindexDurableFunction.StartReindexActivityAsync),
                 Arg.Any<StartReindexActivityInput>());

            await context.DidNotReceive()
                 .CallSubOrchestratorAsync(
                nameof(ReindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync),
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

            context.CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(nameof(ReindexDurableFunction.GetProcessingQueryTagsActivityAsync), reindexOperation.OperationId)
                .Returns(storeEntires);

            await _reindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync(context, NullLogger.Instance);

            // Verify ReindexInstanceActivityAsync is called
            for (int i = 0; i < _reindexConfig.MaxParallelBatches; i++)
            {
                await context.Received().
                     CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstanceActivityAsync),
                         Arg.Is<ReindexInstanceActivityInput>(x => x.StartWatermark == reindexOperation.EndWatermark - _reindexConfig.BatchSize * i - 1
                         && x.EndWatermark == reindexOperation.EndWatermark - _reindexConfig.BatchSize * i));
            }

            // Verify  UpdateReindexProgressActivityAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressActivityAsync),
                    Arg.Is<UpdateReindexProgressActivityInput>(x => x.EndWatermark == reindexOperation.EndWatermark - _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches));


            // Verify  StartNewOrchestration
            context.Received().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync),
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

            context.CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(nameof(ReindexDurableFunction.GetProcessingQueryTagsActivityAsync), reindexOperation.OperationId)
                .Returns(storeEntires);

            await _reindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync(context, NullLogger.Instance);

            // Verify ReindexInstanceActivityAsync is called
            for (int i = 0; i < _reindexConfig.MaxParallelBatches; i++)
            {
                await context.Received().
                     CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstanceActivityAsync),
                         Arg.Is<ReindexInstanceActivityInput>(x => x.StartWatermark == Math.Max(reindexOperation.StartWatermark, reindexOperation.EndWatermark - _reindexConfig.BatchSize * i - 1)
                         && x.EndWatermark == reindexOperation.EndWatermark - _reindexConfig.BatchSize * i));
            }

            // Verify  UpdateReindexProgressActivityAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressActivityAsync),
                    Arg.Is<UpdateReindexProgressActivityInput>(x =>
                    x.EndWatermark == Math.Max(reindexOperation.StartWatermark - 1, reindexOperation.EndWatermark - _reindexConfig.BatchSize * _reindexConfig.MaxParallelBatches)));

            // Verify  CompleteReindexActivityAsync is called
            await context.Received().
                    CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexActivityAsync), reindexOperation.OperationId);

            // Verify StartNewOrchestration is not called
            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync), Arg.Any<ReindexOperation>());
        }

        [Fact]
        public async Task GivenNoInstances_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { StartWatermark = -1, EndWatermark = -1, OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);
            context.CallActivityAsync<IEnumerable<ExtendedQueryTagStoreEntry>>(nameof(ReindexDurableFunction.GetProcessingQueryTagsActivityAsync), reindexOperation.OperationId)
                .Returns(new ExtendedQueryTagStoreEntry[0]);

            await _reindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync(context, NullLogger.Instance);

            await context.Received().
                  CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexActivityAsync), reindexOperation.OperationId);

            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync), Arg.Any<ReindexOperation>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstanceActivityAsync), Arg.Any<ReindexInstanceActivityInput>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressActivityAsync), Arg.Any<UpdateReindexProgressActivityInput>());

        }

        [Fact]
        public async Task GivenNoProcessingTags_WhenCallReindexExtendedQueryTagsOrchestrationAsync_ShouldCompleteReindex()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;

            var storeEntires = new[] { tag.BuildExtendedQueryTagStoreEntry() };
            ReindexOperation reindexOperation = new ReindexOperation() { StartWatermark = 1, EndWatermark = 10, OperationId = Guid.NewGuid().ToString(), StoreEntries = storeEntires };

            IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
            context.GetInput<ReindexOperation>().Returns(reindexOperation);
            await _reindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync(context, NullLogger.Instance);

            await context.Received().
                  CallActivityAsync(nameof(ReindexDurableFunction.CompleteReindexActivityAsync), reindexOperation.OperationId);

            context.DidNotReceive().StartNewOrchestration(nameof(ReindexDurableFunction.ReindexExtendedQueryTagsOrchestrationAsync), Arg.Any<ReindexOperation>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.GetProcessingQueryTagsActivityAsync), Arg.Any<string>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.ReindexInstanceActivityAsync), Arg.Any<ReindexInstanceActivityInput>());
            await context.DidNotReceive().CallActivityAsync(nameof(ReindexDurableFunction.UpdateReindexProgressActivityAsync), Arg.Any<UpdateReindexProgressActivityInput>());
        }
    }
}
