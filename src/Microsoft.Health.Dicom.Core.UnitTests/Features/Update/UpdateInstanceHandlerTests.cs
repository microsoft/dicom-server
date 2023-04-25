// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Update;

public class UpdateInstanceHandlerTests
{
    private const string DefaultContentType = "application/json";
    private readonly UpdateInstanceHandler _handler;
    private readonly IUpdateInstanceOperationService _updateInstanceOperationService;
    private readonly IAuthorizationService<DataActions> _auth;

    public UpdateInstanceHandlerTests()
    {
        _updateInstanceOperationService = Substitute.For<IUpdateInstanceOperationService>();
        _auth = Substitute.For<IAuthorizationService<DataActions>>();
        _handler = new UpdateInstanceHandler(_auth, _updateInstanceOperationService);
    }

    [Fact]
    public async Task GivenNullRequestBody_WhenHandled_ThenArgumentNullExceptionShouldBeThrown()
    {
        var updateInstanceRequest = new UpdateInstanceRequest(null);
        _auth.CheckAccess(DataActions.Write, CancellationToken.None).Returns(DataActions.Write);
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(updateInstanceRequest, CancellationToken.None));
    }

    [Fact]
    public async Task GivenNoAccess_WhenHandlingRequest_ThenThrowUnauthorizedDicomActionException()
    {
        IReadOnlyList<string> studyInstanceUids = new List<string>() { "1.2.3.4" };
        DicomDataset changeDataset = new DicomDataset();
        var updateInstanceRequest = new UpdateInstanceRequest(new UpdateSpecification(studyInstanceUids, changeDataset));
        _auth.CheckAccess(DataActions.Write, CancellationToken.None).Returns(DataActions.None);
        await Assert.ThrowsAsync<UnauthorizedDicomActionException>(() => _handler.Handle(updateInstanceRequest, CancellationToken.None));

        await _auth.Received(1).CheckAccess(DataActions.Write, CancellationToken.None);
        await _updateInstanceOperationService.DidNotReceiveWithAnyArgs().QueueUpdateOperationAsync(default, default);
    }

    [Fact]
    public async Task GivenSupportedContentType_WhenHandled_ThenCorrectUpdateInstanceResponseShouldBeReturned()
    {
        var id = Guid.NewGuid();
        IUrlResolver urlResolver = new MockUrlResolver();
        IReadOnlyList<string> studyInstanceUids = new List<string>() { "1.2.3.4" };
        DicomDataset changeDataset = new DicomDataset();
        var updateSpec = new UpdateSpecification(studyInstanceUids, changeDataset);
        var operation = new OperationReference(id, urlResolver.ResolveOperationStatusUri(id));
        var updateInstanceRequest = new UpdateInstanceRequest(updateSpec);
        var updateInstanceResponse = new UpdateInstanceResponse(operation);
        _auth.CheckAccess(DataActions.Write, CancellationToken.None).Returns(DataActions.Write);

        _updateInstanceOperationService.QueueUpdateOperationAsync(updateSpec, CancellationToken.None).Returns(updateInstanceResponse);

        var response = await _handler.Handle(updateInstanceRequest, CancellationToken.None);

        await _auth.Received(1).CheckAccess(DataActions.Write, CancellationToken.None);
        Assert.Equal(operation, response.Operation);
    }
}
