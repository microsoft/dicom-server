// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class ExportHandlerTests
{
    private readonly IAuthorizationService<DataActions> _auth;
    private readonly IExportService _export;
    private readonly ExportHandler _handler;

    public ExportHandlerTests()
    {
        _auth = Substitute.For<IAuthorizationService<DataActions>>();
        _export = Substitute.For<IExportService>();
        _handler = new ExportHandler(_auth, _export);
    }

    [Fact]
    public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ExportHandler(null, _export));
        Assert.Throws<ArgumentNullException>(() => new ExportHandler(_auth, null));
    }

    [Fact]
    public async Task GivenNullRequest_WhenHandlingRequest_ThenThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null, default));

        await _auth.DidNotReceiveWithAnyArgs().CheckAccess(default, default);
        await _export.DidNotReceiveWithAnyArgs().StartExportAsync(default, default);
    }

    [Fact]
    public async Task GivenNoAccess_WhenHandlingRequest_ThenThrowUnauthorizedDicomActionException()
    {
        using var tokenSource = new CancellationTokenSource();

        _auth.CheckAccess(DataActions.Export, tokenSource.Token).Returns(DataActions.None);
        await Assert.ThrowsAsync<UnauthorizedDicomActionException>(
            () => _handler.Handle(new ExportRequest(new ExportSpecification()), tokenSource.Token));

        await _auth.Received(1).CheckAccess(DataActions.Export, tokenSource.Token);
        await _export.DidNotReceiveWithAnyArgs().StartExportAsync(default, default);
    }

    [Fact]
    public async Task GivenRequest_WhenHandlingRequest_ThenReturnResponse()
    {
        using var tokenSource = new CancellationTokenSource();
        var request = new ExportRequest(new ExportSpecification());
        var expected = new OperationReference(Guid.NewGuid(), new Uri("http://operation"));

        _auth.CheckAccess(DataActions.Export, tokenSource.Token).Returns(DataActions.Export);
        _export.StartExportAsync(request.Specification, tokenSource.Token).Returns(expected);

        ExportResponse response = await _handler.Handle(request, tokenSource.Token);
        Assert.Same(expected, response.Operation);

        await _auth.Received(1).CheckAccess(DataActions.Export, tokenSource.Token);
        await _export
            .Received(1)
            .StartExportAsync(request.Specification, tokenSource.Token);
    }
}
