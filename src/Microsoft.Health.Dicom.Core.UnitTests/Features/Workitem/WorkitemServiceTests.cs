// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class WorkitemServiceTests
    {
        private readonly IWorkitemDatasetValidator _datasetValidator = Substitute.For<IWorkitemDatasetValidator>();
        private readonly IWorkitemResponseBuilder _responseBuilder = Substitute.For<IWorkitemResponseBuilder>();
        private readonly IElementMinimumValidator _minimumValidator = Substitute.For<IElementMinimumValidator>();
        private readonly IWorkitemOrchestrator _storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
        private readonly ILogger<WorkitemService> _logger = Substitute.For<ILogger<WorkitemService>>();
        private readonly DicomDataset _dataset = new DicomDataset();
        private readonly WorkitemService _target;

        public WorkitemServiceTests()
        {
            _datasetValidator.Name.Returns(typeof(AddWorkitemDatasetValidator).Name);

            _target = new WorkitemService(
                _responseBuilder,
                new[] { _datasetValidator },
                _storeOrchestrator,
                _minimumValidator,
                _logger);
        }

        [Fact]
        public async Task GivenNullDicomDataset_WhenProcessed_ThenArgumentNullExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _target.ProcessAddAsync(null, string.Empty, CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task GivenValidWorkitemInstanceUid_WhenProcessed_ThenIsSetupForAffectedSOPInstanceUIDTagInTheDataset()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            await _target.ProcessAddAsync(_dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(workitemInstanceUid, _dataset.GetString(DicomTag.AffectedSOPInstanceUID));
        }

        [Fact]
        public async Task GivenValidWorkitemInstanceUidInDicomTagAffectedSOPInstanceUID_WhenProcessed_ThenIsSetupForSOPInstanceUIDTagInTheDataset()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(workitemInstanceUid, _dataset.GetString(DicomTag.AffectedSOPInstanceUID));
        }

        [Fact]
        public async Task GivenValidDicomDataset_WhenProcessed_ThenCallsValidate()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            await _target.ProcessAddAsync(_dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

            _datasetValidator
                .Received()
                .Validate(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Is<string>(uid => uid == workitemInstanceUid));
        }

        [Fact]
        public async Task GivenValidateThrowsDicomValidationException_WhenProcessed_ThenWorkitemOrchestratorAddWorkitemIsNotCalled()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            await _storeOrchestrator
                .DidNotReceive()
                .AddWorkitemAsync(Arg.Any<DicomDataset>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenValidateThrowsDatasetValidationException_WhenProcessed_ThenWorkitemOrchestratorAddWorkitemIsNotCalled()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DatasetValidationException(ushort.MinValue, string.Empty));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            await _storeOrchestrator
                .DidNotReceive()
                .AddWorkitemAsync(Arg.Any<DicomDataset>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenValidateThrowsDatasetValidationException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var failureCode = FailureReasonCodes.ValidationFailure;
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DatasetValidationException(failureCode, string.Empty));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Is<ushort>(fc => fc == failureCode));
        }

        [Fact]
        public async Task GivenValidateThrowsDicomValidationException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Is<ushort>(fc => fc == FailureReasonCodes.ValidationFailure));
        }

        [Fact]
        public async Task GivenValidateThrowsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalledWithProcessingFailureError()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new Exception(workitemInstanceUid));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Is<ushort>(fc => fc == FailureReasonCodes.ProcessingFailure));
        }

        [Fact]
        public async Task GivenWorkitemOrchestratorThrowsWorkitemAlreadyExistsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var failureCode = FailureReasonCodes.SopInstanceAlreadyExists;

            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _storeOrchestrator
                .When(orc => orc.AddWorkitemAsync(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Any<CancellationToken>()))
                .Throw(new WorkitemAlreadyExistsException(workitemInstanceUid));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Is<ushort>(fc => fc == failureCode));
        }

        [Fact]
        public async Task GivenWorkitemOrchestratorThrowsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var failureCode = FailureReasonCodes.ProcessingFailure;

            var workitemInstanceUid = DicomUID.Generate().UID;

            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _storeOrchestrator
                .When(orc => orc.AddWorkitemAsync(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Any<CancellationToken>()))
                .Throw(new Exception(workitemInstanceUid));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Is<ushort>(fc => fc == failureCode));
        }

        [Fact]
        public async Task GivenDicomDataset_WhenProcessed_ThenResponseBuilderBuildResponseIsAlwaysCalled()
        {
            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            await _target.ProcessAddAsync(new DicomDataset(), string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder.Received().BuildAddResponse();

            _datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder.Received().BuildAddResponse();
        }

        [Fact]
        public async Task GivenWorkitemStoreSucceeded_WhenProcessed_ThenResponseBuilderAddSuccessIsCalled()
        {
            await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            _responseBuilder.Received().AddSuccess(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
        }
    }
}
