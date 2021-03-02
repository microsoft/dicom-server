// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;
using DicomValidationException = Dicom.DicomValidationException;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store
{
    public class DicomStoreServiceTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;
        private static readonly StoreResponse DefaultResponse = new StoreResponse(StoreResponseStatus.Success, new DicomDataset());

        private readonly DicomDataset _dicomDataset1 = Samples.CreateRandomInstanceDataset(
            studyInstanceUid: "1",
            seriesInstanceUid: "2",
            sopInstanceUid: "3",
            sopClassUid: "4");

        private readonly DicomDataset _dicomDataset2 = Samples.CreateRandomInstanceDataset(
            studyInstanceUid: "10",
            seriesInstanceUid: "11",
            sopInstanceUid: "12",
            sopClassUid: "13");

        private readonly IStoreResponseBuilder _storeResponseBuilder = Substitute.For<IStoreResponseBuilder>();
        private readonly IDicomDatasetValidator _dicomDatasetValidator = Substitute.For<IDicomDatasetValidator>();
        private readonly IStoreOrchestrator _storeOrchestrator = Substitute.For<IStoreOrchestrator>();
        private readonly ICustomTagStore _customTagStore = Substitute.For<ICustomTagStore>();
        private readonly StoreService _storeService;

        public DicomStoreServiceTests()
        {
            _storeResponseBuilder.BuildResponse(Arg.Any<string>()).Returns(DefaultResponse);

            _storeService = new StoreService(
                _storeResponseBuilder,
                _dicomDatasetValidator,
                _storeOrchestrator,
                _customTagStore,
                NullLogger<StoreService>.Instance);

            _customTagStore.GetCustomTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<CustomTagEntry>());
        }

        [Fact]
        public async Task GivenNullDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
        {
            await ExecuteAndValidateAsync(dicomInstanceEntries: null);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
        }

        [Fact]
        public async Task GivenEmptyDicomInstanceEntries_WhenProcessed_ThenNoContentShouldBeReturned()
        {
            await ExecuteAndValidateAsync(new IDicomInstanceEntry[0]);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
        }

        [Fact]
        public async Task GivenAValidDicomInstanceEntry_WhenProcessed_ThenSuccessfulEntryShouldBeAdded()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);

            await ExecuteAndValidateAsync(dicomInstanceEntry);

            _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1);
            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddFailure(default);
        }

        [Fact]
        public async Task GiveAnInvalidDicomDataset_WhenProcessed_ThenFailedEntryShouldBeAddedWithProcessingFailure()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new Exception());

            await ExecuteAndValidateAsync(dicomInstanceEntry);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.Received(1).AddFailure(null, TestConstants.ProcessingFailureReasonCode);
        }

        [Fact]
        public async Task GivenADicomDatasetFailsToOpenDueToDicomValidationException_WhenProcessed_ThenFailedEntryShouldBeAddedWithValidationFailure()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns<DicomDataset>(_ => throw new DicomValidationException("value", DicomVR.UI, string.Empty));

            await ExecuteAndValidateAsync(dicomInstanceEntry);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.Received(1).AddFailure(null, TestConstants.ValidationFailureReasonCode);
        }

        [Fact]
        public async Task GivenAValidationError_WhenProcessed_ThenFailedEntryShouldBeAddedWithValidationFailure()
        {
            const ushort failureCode = 500;

            _dicomDatasetValidator
                .When(validator => validator.Validate(Arg.Any<DicomDataset>(), Arg.Any<IReadOnlyList<CustomTagEntry>>(), Arg.Any<string>()))
                .Do(_ => throw new DatasetValidationException(failureCode, "test"));

            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

            await ExecuteAndValidateAsync(dicomInstanceEntry);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, failureCode);
        }

        [Fact]
        public async Task GivenADicomInstanceAlreadyExistsExceptionWithConflictWhenStoring_WhenProcessed_ThenFailedEntryShouldBeAddedWithSopInstanceAlreadyExists()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

            _storeOrchestrator
                .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken))
                .Do(_ => throw new InstanceAlreadyExistsException());

            await ExecuteAndValidateAsync(dicomInstanceEntry);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.SopInstanceAlreadyExistsReasonCode);
        }

        [Fact]
        public async Task GivenAnExceptionWhenStoring_WhenProcessed_ThenFailedEntryShouldBeAddedWithProcessingFailure()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

            _storeOrchestrator
                .When(dicomStoreService => dicomStoreService.StoreDicomInstanceEntryAsync(dicomInstanceEntry, DefaultCancellationToken))
                .Do(_ => throw new DataStoreException("Simulated failure."));

            await ExecuteAndValidateAsync(dicomInstanceEntry);

            _storeResponseBuilder.DidNotReceiveWithAnyArgs().AddSuccess(default);
            _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.ProcessingFailureReasonCode);
        }

        [Fact]
        public async Task GivenMultipleDicomInstanceEntries_WhenProcessed_ThenCorrespondingEntryShouldBeAdded()
        {
            IDicomInstanceEntry dicomInstanceEntryToSucceed = Substitute.For<IDicomInstanceEntry>();
            IDicomInstanceEntry dicomInstanceEntryToFail = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntryToSucceed.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset1);
            dicomInstanceEntryToFail.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

            _dicomDatasetValidator
                .When(dicomDatasetMinimumRequirementValidator => dicomDatasetMinimumRequirementValidator.Validate(_dicomDataset2, Arg.Any<IReadOnlyList<CustomTagEntry>>(), null))
                .Do(_ => throw new Exception());

            await ExecuteAndValidateAsync(dicomInstanceEntryToSucceed, dicomInstanceEntryToFail);

            _storeResponseBuilder.Received(1).AddSuccess(_dicomDataset1);
            _storeResponseBuilder.Received(1).AddFailure(_dicomDataset2, TestConstants.ProcessingFailureReasonCode);
        }

        [Fact]
        public async Task GivenRequiredStudyInstanceUid_WhenProcessed_ThenItShouldBePassed()
        {
            IDicomInstanceEntry dicomInstanceEntry = Substitute.For<IDicomInstanceEntry>();

            dicomInstanceEntry.GetDicomDatasetAsync(DefaultCancellationToken).Returns(_dicomDataset2);

            await ExecuteAndValidateAsync(dicomInstanceEntry);
        }

        private Task ExecuteAndValidateAsync(params IDicomInstanceEntry[] dicomInstanceEntries)
            => ExecuteAndValidateAsync(requiredStudyInstanceUid: null, dicomInstanceEntries);

        private async Task ExecuteAndValidateAsync(
            string requiredStudyInstanceUid,
            params IDicomInstanceEntry[] dicomInstanceEntries)
        {
            StoreResponse response = await _storeService.ProcessAsync(
                dicomInstanceEntries,
                requiredStudyInstanceUid,
                cancellationToken: DefaultCancellationToken);

            Assert.Equal(DefaultResponse, response);

            _storeResponseBuilder.Received(1).BuildResponse(requiredStudyInstanceUid);

            if (dicomInstanceEntries != null)
            {
                foreach (IDicomInstanceEntry dicomInstanceEntry in dicomInstanceEntries)
                {
                    await ValidateDisposeAsync(dicomInstanceEntry);
                }
            }
        }

        private async Task ValidateDisposeAsync(IDicomInstanceEntry dicomInstanceEntry)
        {
            var timeout = DateTime.Now.AddSeconds(5);

            while (timeout < DateTime.Now)
            {
                if (dicomInstanceEntry.ReceivedCalls().Any())
                {
                    await dicomInstanceEntry.Received(1).DisposeAsync();
                    break;
                }

                await Task.Delay(100);
            }
        }
    }
}
