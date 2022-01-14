// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
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
        [Fact]
        public async Task GivenNullDicomDataset_WhenProcessed_ThenArgumentNullExceptionIsThrown()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await target.ProcessAsync(null, string.Empty, CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task GivenValidWorkitemInstanceUid_WhenProcessed_ThenIsSetupForSOPInstanceUIDTagInTheDataset()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();
            var workitemInstanceUid = DicomUID.Generate().UID;

            var dataset = new DicomDataset();

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(workitemInstanceUid, dataset.GetString(DicomTag.SOPInstanceUID));
        }


        [Fact]
        public async Task GivenValidWorkitemInstanceUidInDicomTagAffectedSOPInstanceUID_WhenProcessed_ThenIsSetupForSOPInstanceUIDTagInTheDataset()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();
            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(workitemInstanceUid, dataset.GetString(DicomTag.SOPInstanceUID));
        }

        [Fact]
        public async Task GivenNoWorkitemInstanceUid_WhenProcessed_ThenNewUidIsSetupForSOPInstanceUIDTagInTheDataset()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var dataset = new DicomDataset();

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            Assert.False(string.IsNullOrWhiteSpace(dataset.GetString(DicomTag.SOPInstanceUID)));
        }

        [Fact]
        public async Task GivenValidDicomDataset_WhenProcessed_ThenCallsValidate()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            datasetValidator
                .Received()
                .Validate(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Is<string>(uid => uid == workitemInstanceUid));
        }

        [Fact]
        public async Task GivenValidateThrowsDicomValidationException_WhenProcessed_ThenWorkitemOrchestratorAddWorkitemIsNotCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            await storeOrchestrator
                .DidNotReceive()
                .AddWorkitemAsync(Arg.Any<DicomDataset>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenValidateThrowsDatasetValidationException_WhenProcessed_ThenWorkitemOrchestratorAddWorkitemIsNotCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DatasetValidationException(ushort.MinValue, string.Empty));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            await storeOrchestrator
                .DidNotReceive()
                .AddWorkitemAsync(Arg.Any<DicomDataset>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GivenValidateThrowsDatasetValidationException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var failureCode = FailureReasonCodes.ValidationFailure;
            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DatasetValidationException(failureCode, string.Empty));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Is<ushort>(fc => fc == failureCode));
        }

        [Fact]
        public async Task GivenValidateThrowsDicomValidationException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Is<ushort>(fc => fc == FailureReasonCodes.ValidationFailure));
        }

        [Fact]
        public async Task GivenValidateThrowsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalledWithProcessingFailureError()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new Exception(workitemInstanceUid));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Is<ushort>(fc => fc == FailureReasonCodes.ProcessingFailure));
        }

        [Fact]
        public async Task GivenWorkitemOrchestratorThrowsWorkitemAlreadyExistsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var failureCode = FailureReasonCodes.SopInstanceAlreadyExists;

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            storeOrchestrator
                .When(orc => orc.AddWorkitemAsync(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Any<CancellationToken>()))
                .Throw(new WorkitemAlreadyExistsException(workitemInstanceUid));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Is<ushort>(fc => fc == failureCode));
        }


        [Fact]
        public async Task GivenWorkitemOrchestratorThrowsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var failureCode = FailureReasonCodes.ProcessingFailure;

            var workitemInstanceUid = DicomUID.Generate().UID;
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            storeOrchestrator
                .When(orc => orc.AddWorkitemAsync(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Any<CancellationToken>()))
                .Throw(new Exception(workitemInstanceUid));

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder
                .Received()
                .AddFailure(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)), Arg.Is<ushort>(fc => fc == failureCode));
        }

        [Fact]
        public async Task GivenDicomDataset_WhenProcessed_ThenResponseBuilderBuildResponseIsAlwaysCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            await target.ProcessAsync(new DicomDataset(), string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder.Received().BuildResponse();

            datasetValidator
                .When(dv => dv.Validate(Arg.Any<DicomDataset>(), Arg.Any<string>()))
                .Throw(new DicomValidationException(string.Empty, DicomVR.UN, string.Empty));

            await target.ProcessAsync(new DicomDataset(), string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder.Received().BuildResponse();
        }

        [Fact]
        public async Task GivenWorkitemStoreSucceeded_WhenProcessed_ThenResponseBuilderAddSuccessIsCalled()
        {
            var responseBuilder = Substitute.For<IAddWorkitemResponseBuilder>();
            var datasetValidator = Substitute.For<IAddWorkitemDatasetValidator>();
            var storeOrchestrator = Substitute.For<IWorkitemOrchestrator>();
            var minimumValidator = Substitute.For<IElementMinimumValidator>();
            var logger = Substitute.For<ILogger<WorkitemService>>();
            var dataset = new DicomDataset();

            var target = new WorkitemService(responseBuilder, datasetValidator, storeOrchestrator, minimumValidator, logger);

            await target.ProcessAsync(dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

            responseBuilder.Received().AddSuccess(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, dataset)));
        }
    }
}
