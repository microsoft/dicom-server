// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed;

public class DeleteExtendedQueryTagServiceTests
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IDeleteExtendedQueryTagService _extendedQueryTagService;
    private readonly IGuidFactory _guidFactory = Substitute.For<IGuidFactory>();
    private readonly IDicomOperationsClient _dicomOperationsClient = Substitute.For<IDicomOperationsClient>();
    private readonly IOptions<ExtendedQueryTagConfiguration> _options = Substitute.For<IOptions<ExtendedQueryTagConfiguration>>();
    private readonly ILogger<DeleteExtendedQueryTagService> _logger;

    public DeleteExtendedQueryTagServiceTests()
    {
        _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
        _logger = Substitute.For<ILogger<DeleteExtendedQueryTagService>>();

        ExtendedQueryTagConfiguration extendedQueryTagConfiguration = new ExtendedQueryTagConfiguration
        {
            OperationRetryInterval = TimeSpan.FromSeconds(0),
            OperationRetryCount = 1,
        };

        _options.Value.Returns(extendedQueryTagConfiguration);

        _extendedQueryTagService = new DeleteExtendedQueryTagService(_extendedQueryTagStore, new DicomTagParser(), _guidFactory, _dicomOperationsClient, _options, _logger);
    }

    [Fact]
    public async Task GivenInvalidTagPath_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldThrowException()
    {
        await Assert.ThrowsAsync<InvalidExtendedQueryTagPathException>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync("0000000A"));
    }

    [Fact]
    public async Task GivenNotExistingTagPath_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldPassException()
    {
        string path = DicomTag.DeviceSerialNumber.GetPath();
        _extendedQueryTagStore
            .GetExtendedQueryTagAsync(path, default)
            .Returns(Task.FromException<ExtendedQueryTagStoreJoinEntry>(new ExtendedQueryTagNotFoundException("Tag doesn't exist")));

        await Assert.ThrowsAsync<ExtendedQueryTagNotFoundException>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync(path));

        await _extendedQueryTagStore
            .Received(1)
            .GetExtendedQueryTagAsync(path, default);
    }

    [Fact]
    public async Task GivenTagAlreadyInDeletingState_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldPassException()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        string tagPath = tag.GetPath();
        var entry = new ExtendedQueryTagStoreJoinEntry(tag.BuildExtendedQueryTagStoreEntry());

        _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath, default).Returns(entry);

        _extendedQueryTagStore
            .UpdateExtendedQueryTagStatusToDelete(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExtendedQueryTagEntry>(new ExtendedQueryTagBusyException("Tag is already in deleting state")));

        await Assert.ThrowsAsync<ExtendedQueryTagBusyException>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync(tagPath));

        await _extendedQueryTagStore
            .Received(1)
            .GetExtendedQueryTagAsync(tagPath, default);
    }

    [Fact]
    public async Task GivenValidTagPath_WhenDeleteExtendedQueryTagIsInvoked_ThenShouldSucceed()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        string tagPath = tag.GetPath();
        var entry = new ExtendedQueryTagStoreJoinEntry(tag.BuildExtendedQueryTagStoreEntry());

        var runningOpertaion = GetOperationState(OperationStatus.Running);
        var succeededOperation = GetOperationState(OperationStatus.Succeeded);

        _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath, default).Returns(entry);
        _dicomOperationsClient.GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(x => runningOpertaion, x => succeededOperation);

        await _extendedQueryTagService.DeleteExtendedQueryTagAsync(tagPath);
        await _dicomOperationsClient.Received(1).StartDeleteExtendedQueryTagOperationAsync(Arg.Any<Guid>(), entry.Key, entry.VR, Arg.Any<CancellationToken>());
        await _dicomOperationsClient.Received(2).GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenValidTagPath_WhenDeleteExtendedQueryTagIsInvokedAndOpertaionFails_ThenShouldPassException()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        string tagPath = tag.GetPath();
        var entry = new ExtendedQueryTagStoreJoinEntry(tag.BuildExtendedQueryTagStoreEntry());

        var runningOpertaion = GetOperationState(OperationStatus.Running);
        var failedOperation = GetOperationState(OperationStatus.Failed);

        _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath, default).Returns(entry);
        _dicomOperationsClient.GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(x => runningOpertaion, x => failedOperation);

        await Assert.ThrowsAsync<DataStoreException>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync(tagPath));

        await _dicomOperationsClient.Received(1).StartDeleteExtendedQueryTagOperationAsync(Arg.Any<Guid>(), entry.Key, entry.VR, Arg.Any<CancellationToken>());
        await _dicomOperationsClient.Received(2).GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenValidTagPath_WhenDeleteExtendedQueryTagIsInvokedAndWaitExceedsRetries_ThenShouldPassException()
    {
        DicomTag tag = DicomTag.DeviceSerialNumber;
        string tagPath = tag.GetPath();
        var entry = new ExtendedQueryTagStoreJoinEntry(tag.BuildExtendedQueryTagStoreEntry());

        var runningOpertaion = GetOperationState(OperationStatus.Running);
        var runningOpertaion2 = GetOperationState(OperationStatus.Running);
        var runningOpertaion3 = GetOperationState(OperationStatus.Running);

        _extendedQueryTagStore.GetExtendedQueryTagAsync(tagPath, default).Returns(entry);
        _dicomOperationsClient.GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(x => runningOpertaion, x => runningOpertaion2, x => runningOpertaion3);

        await Assert.ThrowsAsync<DataStoreException>(() => _extendedQueryTagService.DeleteExtendedQueryTagAsync(tagPath));

        await _dicomOperationsClient.Received(1).StartDeleteExtendedQueryTagOperationAsync(Arg.Any<Guid>(), entry.Key, entry.VR, Arg.Any<CancellationToken>());
        await _dicomOperationsClient.Received(2).GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static OperationState<DicomOperation> GetOperationState(OperationStatus status)
    {
        return new OperationState<DicomOperation>
        {
            CreatedTime = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedTime = DateTime.UtcNow,
            OperationId = Guid.NewGuid(),
            PercentComplete = 100,
            Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
            Status = status,
            Type = DicomOperation.DeleteExtendedQueryTag,
        };
    }
}
