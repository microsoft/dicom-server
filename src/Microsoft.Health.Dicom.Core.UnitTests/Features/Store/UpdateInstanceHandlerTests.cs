// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Update;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;

public class UpdateInstanceHandlerTests
{
    private const string DefaultContentType = "application/json";
    private readonly UpdateInstanceHandler _handler;
    private readonly IUpdateInstanceService _updateInstanceService;
    private readonly IAuthorizationService<DataActions> _auth;

    public UpdateInstanceHandlerTests()
    {
        _updateInstanceService = Substitute.For<IUpdateInstanceService>();
        _auth = Substitute.For<IAuthorizationService<DataActions>>();
        _handler = new UpdateInstanceHandler(_auth, _updateInstanceService);
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
        var updateInstanceRequest = new UpdateInstanceRequest(new UpdateSpecification());
        _auth.CheckAccess(DataActions.Write, CancellationToken.None).Returns(DataActions.None);
        await Assert.ThrowsAsync<UnauthorizedDicomActionException>(() => _handler.Handle(updateInstanceRequest, CancellationToken.None));

        await _auth.Received(1).CheckAccess(DataActions.Write, CancellationToken.None);
        await _updateInstanceService.DidNotReceiveWithAnyArgs().UpdateInstanceAsync(default, default);
    }

    [Fact]
    public async Task GivenSupportedContentType_WhenHandled_ThenCorrectUpdateInstanceResponseShouldBeReturned()
    {
        Guid id = Guid.NewGuid();
        IUrlResolver urlResolver = new MockUrlResolver();
        var updateSpec = new UpdateSpecification();
        OperationReference operation = new OperationReference(id, urlResolver.ResolveOperationStatusUri(id));
        var updateInstanceRequest = new UpdateInstanceRequest(updateSpec);

        _auth.CheckAccess(DataActions.Write, CancellationToken.None).Returns(DataActions.Write);

        _updateInstanceService.UpdateInstanceAsync(updateSpec, CancellationToken.None).Returns(operation);

        var response = await _handler.Handle(updateInstanceRequest, CancellationToken.None);

        await _auth.Received(1).CheckAccess(DataActions.Write, CancellationToken.None);
        Assert.Equal(operation, response.Operation);
    }
}
