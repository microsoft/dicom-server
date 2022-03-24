// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Operations;

public class OperationStateHandlerTests
{
    [Fact]
    public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationStateHandler(null, Substitute.For<IDicomOperationsClient>()));
        Assert.Throws<ArgumentNullException>(() => new OperationStateHandler(Substitute.For<IAuthorizationService<DataActions>>(), null));
    }

    [Fact]
    public async Task GivenInvalidAuthorization_WhenHandlingRequest_ThenThrowUnauthorizedDicomActionException()
    {
        using var source = new CancellationTokenSource();
        IAuthorizationService<DataActions> auth = Substitute.For<IAuthorizationService<DataActions>>();
        IDicomOperationsClient client = Substitute.For<IDicomOperationsClient>();
        var handler = new OperationStateHandler(auth, client);

        auth.CheckAccess(DataActions.Read, source.Token).Returns(DataActions.None);

        await Assert.ThrowsAsync<UnauthorizedDicomActionException>(() => handler.Handle(new OperationStateRequest(Guid.NewGuid()), source.Token));

        await auth.Received(1).CheckAccess(DataActions.Read, source.Token);
        await client.DidNotReceiveWithAnyArgs().GetStateAsync(default, default);
    }

    [Fact]
    public async Task GivenValidRequest_WhenHandlingRequest_ThenReturnResponse()
    {
        using var source = new CancellationTokenSource();
        IAuthorizationService<DataActions> auth = Substitute.For<IAuthorizationService<DataActions>>();
        IDicomOperationsClient client = Substitute.For<IDicomOperationsClient>();
        var handler = new OperationStateHandler(auth, client);

        Guid id = Guid.NewGuid();
        var expected = new OperationState<DicomOperation>
        {
            CreatedTime = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedTime = DateTime.UtcNow,
            OperationId = id,
            PercentComplete = 100,
            Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
            Status = OperationStatus.Completed,
            Type = DicomOperation.Reindex,
        };

        auth.CheckAccess(DataActions.Read, source.Token).Returns(DataActions.Read);
        client.GetStateAsync(id, source.Token).Returns(expected);

        Assert.Same(expected, (await handler.Handle(new OperationStateRequest(id), source.Token)).OperationState);

        await auth.Received(1).CheckAccess(DataActions.Read, source.Token);
        await client.Received(1).GetStateAsync(id, source.Token);
    }
}
