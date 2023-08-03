// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem;

public sealed class WorkitemServiceTests
{
    private readonly IWorkitemDatasetValidator _addDatasetValidator = Substitute.For<IWorkitemDatasetValidator>();
    private readonly IWorkitemDatasetValidator _cancelDatasetValidator = Substitute.For<IWorkitemDatasetValidator>();
    private readonly IWorkitemResponseBuilder _responseBuilder = Substitute.For<IWorkitemResponseBuilder>();
    private readonly IWorkitemOrchestrator _orchestrator = Substitute.For<IWorkitemOrchestrator>();
    private readonly ILogger<WorkitemService> _logger = Substitute.For<ILogger<WorkitemService>>();
    private readonly DicomDataset _dataset = new DicomDataset();
    private readonly WorkitemService _target;

    public WorkitemServiceTests()
    {
        _addDatasetValidator.Name.Returns(typeof(AddWorkitemDatasetValidator).Name);
        _cancelDatasetValidator.Name.Returns(typeof(CancelWorkitemDatasetValidator).Name);

        _target = new WorkitemService(_responseBuilder, new[]
        {
            _addDatasetValidator,
            _cancelDatasetValidator
        }, _orchestrator, _logger);

        _dataset.Add(DicomTag.ProcedureStepState, string.Empty);
    }

    [Fact]
    public async Task GivenNullDicomDataset_WhenProcessed_ThenArgumentNullExceptionIsThrown()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _target.ProcessAddAsync(null, string.Empty, CancellationToken.None).ConfigureAwait(false));
    }

    [Fact]
    public async Task GivenValidWorkitemInstanceUid_WhenProcessed_ThenIsSetupForSOPInstanceUIDTagInTheDataset()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        await _target.ProcessAddAsync(_dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

        Assert.Equal(workitemInstanceUid, _dataset.GetString(DicomTag.SOPInstanceUID));
    }

    [Fact]
    public async Task GivenValidWorkitemInstanceUidInDicomTagSOPInstanceUID_WhenProcessed_ThenIsSetupForSOPInstanceUIDTagInTheDataset()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        Assert.Equal(workitemInstanceUid, _dataset.GetString(DicomTag.SOPInstanceUID));
    }

    [Fact]
    public async Task GivenValidDicomDataset_WhenProcessed_ThenCallsValidate()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        await _target.ProcessAddAsync(_dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

        _addDatasetValidator
            .Received()
            .Validate(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenValidateThrowsDatasetValidationException_WhenProcessed_ThenWorkitemOrchestratorAddWorkitemIsNotCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _addDatasetValidator
            .When(dv => dv.Validate(Arg.Any<DicomDataset>()))
            .Throw(new DatasetValidationException(ushort.MinValue, string.Empty));

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        await _orchestrator
            .DidNotReceive()
            .AddWorkitemAsync(Arg.Any<DicomDataset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenValidateThrowsDatasetValidationException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
    {
        var failureCode = FailureReasonCodes.ValidationFailure;
        var workitemInstanceUid = DicomUID.Generate().UID;
        var errorMessage = @"Unit Test - Failed validation";

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _addDatasetValidator
            .When(dv => dv.Validate(Arg.Any<DicomDataset>()))
            .Throw(new DatasetValidationException(failureCode, errorMessage));

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == failureCode),
                Arg.Is<string>(msg => msg == errorMessage),
                Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenValidateThrowsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalledWithProcessingFailureError()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        var errorMessage = @"Unit Test - Failed validation";

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _addDatasetValidator
            .When(dv => dv.Validate(Arg.Any<DicomDataset>()))
            .Throw(new Exception(errorMessage));

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.ProcessingFailure),
                Arg.Is<string>(msg => msg == errorMessage),
                Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenWorkitemOrchestratorThrowsWorkitemAlreadyExistsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
    {
        var failureCode = FailureReasonCodes.SopInstanceAlreadyExists;

        var workitemInstanceUid = DicomUID.Generate().UID;

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _orchestrator
            .When(orc => orc.AddWorkitemAsync(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Any<CancellationToken>()))
            .Throw(new WorkitemAlreadyExistsException());

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == failureCode),
                Arg.Is<string>(msg => msg == DicomCoreResource.WorkitemInstanceAlreadyExists),
                Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenWorkitemOrchestratorThrowsException_WhenProcessed_ThenResponseBuilderAddFailureIsCalled()
    {
        var failureCode = FailureReasonCodes.ProcessingFailure;

        var workitemInstanceUid = DicomUID.Generate().UID;

        _dataset.Add(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _orchestrator
            .When(orc => orc.AddWorkitemAsync(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)), Arg.Any<CancellationToken>()))
            .Throw(new Exception(workitemInstanceUid));

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == failureCode),
                Arg.Is<string>(msg => msg == workitemInstanceUid),
                Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenDicomDataset_WhenProcessed_ThenResponseBuilderBuildResponseIsAlwaysCalled()
    {
        _addDatasetValidator
            .When(dv => dv.Validate(Arg.Any<DicomDataset>()))
            .Throw(new ElementValidationException(string.Empty, DicomVR.UN, ValidationErrorCode.UnexpectedVR));

        await _target.ProcessAddAsync(new DicomDataset(), string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder.Received().BuildAddResponse();

        _addDatasetValidator
            .When(dv => dv.Validate(Arg.Any<DicomDataset>()))
            .Throw(new ElementValidationException(string.Empty, DicomVR.UN, ValidationErrorCode.UnexpectedVR));

        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder.Received().BuildAddResponse();
    }

    [Fact]
    public async Task GivenWorkitemStoreSucceeded_WhenProcessed_ThenResponseBuilderAddSuccessIsCalled()
    {
        await _target.ProcessAddAsync(_dataset, string.Empty, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder.Received().AddSuccess(Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenProcessCancel_WhenWorkitemIsNotFound_ThenResponseBuilderAddFailureIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(null as WorkitemMetadataStoreEntry));

        await _target.ProcessCancelAsync(_dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder.Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.UpsInstanceNotFound),
                Arg.Is<string>(v => string.Equals(v, DicomCoreResource.WorkitemInstanceNotFound)),
                Arg.Is<DicomDataset>(ds => ReferenceEquals(ds, _dataset)));
    }

    [Fact]
    public async Task GivenProcessCancel_WhenWorkitemIsNotFound_ThenResponseBuilderBuildResponseIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(null as WorkitemMetadataStoreEntry));

        await _target.ProcessCancelAsync(_dataset, workitemInstanceUid, CancellationToken.None).ConfigureAwait(false);

        _responseBuilder.Received().BuildCancelResponse();
    }

    [Fact]
    public async Task GivenProcessCancel_WhenGetWorkitemBlobAsyncFails_Throws()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)));

        _orchestrator
            .When(orc => orc.GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>()))
            .Throw(new ArgumentException(@"Error thrown from the test mock"));

        await Assert.ThrowsAsync<ArgumentException>(() => _target.ProcessCancelAsync(_dataset, workitemInstanceUid, CancellationToken.None));
    }

    [Fact]
    public async Task GivenProcessCancel_WhenValidationSucceeds_ThenResponseBuilderAddSuccessIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder.Received().AddSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task GivenProcessCancel_AlwaysCallsCancelWorkitemDatasetValidator()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _cancelDatasetValidator.Received().Validate(Arg.Any<DicomDataset>());
    }

    [Fact]
    public async Task GivenProcessCancel_WhenCancelWorkitemDatasetValidatorFailsWithDatasetValidationException_ThenResponseBuilderAddFailureIsCalled()
    {
        var failureCode = FailureReasonCodes.UpsIsAlreadyCanceled;
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        _cancelDatasetValidator
            .When(v => v.Validate(Arg.Any<DicomDataset>()))
            .Throw(new DatasetValidationException(failureCode, @"Failure from Unit Test"));

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == failureCode),
                Arg.Is<string>(msg => msg == @"Failure from Unit Test"),
                Arg.Any<DicomDataset>());
    }

    [Fact]
    public async Task GivenProcessCancel_WhenCancelWorkitemDatasetValidatorFailsWithDicomValidationException_ThenResponseBuilderAddFailureIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        _cancelDatasetValidator
            .When(v => v.Validate(Arg.Any<DicomDataset>()))
            .Throw(new DicomValidationException(@"Error content", DicomVR.ST, @"Failure from Unit Test"));

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.UpsInstanceUpdateNotAllowed),
                Arg.Is<string>(msg => msg == "Content \"Error content\" does not validate VR ST: Failure from Unit Test"),
                Arg.Any<DicomDataset>());
    }

    [Fact]
    public async Task GivenProcessCancel_WhenCancelWorkitemDatasetValidatorFailsWithValidationException_ThenResponseBuilderAddFailureIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        _cancelDatasetValidator
            .When(v => v.Validate(Arg.Any<DicomDataset>()))
            .Throw(Substitute.For<ValidationException>(@"Failure from Unit Test"));

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.UpsInstanceUpdateNotAllowed),
                Arg.Any<string>(),
                Arg.Any<DicomDataset>());
    }

    [Fact]
    public async Task GivenProcessCancel_WhenCancelWorkitemDatasetValidatorFailsWithWorkitemNotFoundException_ThenResponseBuilderAddFailureIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        var exception = new WorkitemNotFoundException();
        _cancelDatasetValidator
            .When(v => v.Validate(Arg.Any<DicomDataset>()))
            .Throw(exception);

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.UpsInstanceNotFound),
                Arg.Is<string>(msg => msg == exception.Message),
                Arg.Any<DicomDataset>());
    }

    [Fact]
    public async Task GivenProcessCancel_WhenCancelWorkitemDatasetValidatorFailsWithUnknownException_ThenResponseBuilderAddFailureIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        var exception = new Exception(@"Failure from unit test");
        _cancelDatasetValidator
            .When(v => v.Validate(Arg.Any<DicomDataset>()))
            .Throw(exception);

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");
        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.ProcessingFailure),
                Arg.Is<string>(msg => msg == exception.Message),
                Arg.Any<DicomDataset>());
    }

    [Fact]
    public async Task GivenProcessCancel_WhenUpdateWorkitemFails_ThenResponseBuilderAddFailureIsCalled()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;
        _dataset.AddOrUpdate(DicomTag.SOPInstanceUID, workitemInstanceUid);
        _dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkitemMetadataStoreEntry(workitemInstanceUid, 1, 101, Partition.DefaultKey)
            {
                ProcedureStepState = ProcedureStepState.Scheduled,
                Status = WorkitemStoreStatus.ReadWrite
            }));

        _orchestrator
            .GetWorkitemBlobAsync(Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_dataset));

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from Unit Test");

        _orchestrator
            .When(orc => orc.UpdateWorkitemStateAsync(Arg.Any<DicomDataset>(), Arg.Any<WorkitemMetadataStoreEntry>(), Arg.Any<ProcedureStepState>(), Arg.Any<CancellationToken>()))
            .Throw(new Exception(@"Failure from unit tests"));

        await _target.ProcessCancelAsync(cancelRequestDataset, workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.ProcessingFailure),
                Arg.Any<string>(),
                Arg.Any<DicomDataset>());
    }

    [Theory]
    [MemberData(nameof(AddedAttributesForCreate))]
    internal void GivenEmptyDataset_AttributesAreAdded(DicomTag tag)
    {
        var dataset = new DicomDataset();

        WorkitemService.SetSpecifiedAttributesForCreate(dataset, string.Empty);

        Assert.True(dataset.Contains(tag));
        Assert.True(dataset.GetValueCount(tag) > 0);
    }

    public static IEnumerable<object[]> AddedAttributesForCreate()
    {
        yield return new object[] { DicomTag.SOPClassUID };
        yield return new object[] { DicomTag.ScheduledProcedureStepModificationDateTime };
        yield return new object[] { DicomTag.WorklistLabel };
        yield return new object[] { DicomTag.ProcedureStepState };
    }

    [Fact]
    internal void GivenQueryParam_AndNoDatasetAttribute_QueryParamIsSet()
    {
        var dataset = new DicomDataset();

        WorkitemService.ReconcileWorkitemInstanceUid(dataset, "123");

        Assert.Equal("123", dataset.GetString(DicomTag.SOPInstanceUID));
    }

    [Fact]
    internal void GivenQueryParam_AndMatchingDatasetAttribute_QueryParamIsSet()
    {
        var dataset = new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "123"));

        WorkitemService.ReconcileWorkitemInstanceUid(dataset, "123");

        Assert.Equal("123", dataset.GetString(DicomTag.SOPInstanceUID));
    }

    [Fact]
    internal void GivenQueryParam_AndDifferentDatasetAttribute_Throws()
    {
        var dataset = new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "123"));

        Assert.Throws<DatasetValidationException>(() => WorkitemService.ReconcileWorkitemInstanceUid(dataset, "456"));
    }

    [Fact]
    internal void GivenNoQueryParam_AndDatasetAttribute_AttributeIsSet()
    {
        var dataset = new DicomDataset(new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "123"));

        WorkitemService.ReconcileWorkitemInstanceUid(dataset, null);

        Assert.Equal("123", dataset.GetString(DicomTag.SOPInstanceUID));
    }

    [Fact]
    internal void GivenNoQueryParam_AndNoDatasetAttribute_NothingIsSet()
    {
        var dataset = new DicomDataset();

        WorkitemService.ReconcileWorkitemInstanceUid(dataset, null);

        Assert.False(dataset.Contains(DicomTag.SOPInstanceUID));
    }

    [Fact]
    public async Task GivenNoWorkitem_ProcessRetrieveAsync_ReturnsNotFoundResponseStatus()
    {
        var workitemInstanceUid = DicomUID.Generate().UID;

        _orchestrator
            .GetWorkitemMetadataAsync(Arg.Is<string>(uid => string.Equals(workitemInstanceUid, uid)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(null as WorkitemMetadataStoreEntry));

        var response = await _target.ProcessRetrieveAsync(workitemInstanceUid, CancellationToken.None);

        _responseBuilder
            .Received()
            .AddFailure(
                Arg.Is<ushort>(fc => fc == FailureReasonCodes.UpsInstanceNotFound),
                Arg.Is<string>(msg => msg == DicomCoreResource.WorkitemInstanceNotFound));
    }

    private static QueryParameters CreateParameters(
        Dictionary<string, string> filters,
        QueryResource resourceType,
        string studyInstanceUid = null,
        string seriesInstanceUid = null,
        bool fuzzyMatching = false,
        string[] includeField = null)
    {
        return new QueryParameters
        {
            Filters = filters,
            FuzzyMatching = fuzzyMatching,
            IncludeField = includeField ?? Array.Empty<string>(),
            QueryResourceType = resourceType,
            SeriesInstanceUid = seriesInstanceUid,
            StudyInstanceUid = studyInstanceUid,
        };
    }
}
