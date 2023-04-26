// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------



using EnsureThat;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Security;
using NSubstitute;
using Xunit;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Exceptions;
using System.Threading;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;
public class RetrieveRenderedHandlerTests
{

    private readonly IRetrieveRenderedService _retrieveRenderedService;
    private readonly RetrieveRenderedHandler _retrieveRenderedHandler;

    public RetrieveRenderedHandlerTests()
    {
        _retrieveRenderedService = Substitute.For<IRetrieveRenderedService>();
        _retrieveRenderedHandler = new RetrieveRenderedHandler(new DisabledAuthorizationService<DataActions>(), _retrieveRenderedService);
    }

    [Theory]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("345%^&")]
    [InlineData("()")]
    public async Task GivenARequestWithInvalidStudyInstanceIdentifier_WhenHandlerIsExecuted_ThenDicomInvalidIdentifierExceptionIsThrown(string studyInstanceUid)
    {
        EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Instance, 0, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(ValidationErrorCode.UidIsInvalid, ex.ErrorCode);
    }


    [Theory]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("345%^&")]
    [InlineData("()")]
    public async Task GivenARequestWithInvalidSeriesInstanceIdentifier_WhenHandlerIsExecuted_ThenDicomInvalidIdentifierExceptionIsThrown(string seriesInstanceUid)
    {
        EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
        string studyInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Instance, 0, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(ValidationErrorCode.UidIsInvalid, ex.ErrorCode);
    }

    [Theory]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("345%^&")]
    [InlineData("()")]
    public async Task GivenARequestWithInvalidInstanceInstanceIdentifier_WhenHandlerIsExecuted_ThenDicomInvalidIdentifierExceptionIsThrown(string sopInstanceUid)
    {
        EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Instance, 0, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(ValidationErrorCode.UidIsInvalid, ex.ErrorCode);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(-3)]
    public async Task GivenARequestWithInvalidFramNumber_WhenHandlerIsExecuted_ThenBadRequestExceptionIsThrown(int frame)
    {
        string error = "The specified frames value is not valid. At least one frame must be present, and all requested frames must have value greater than 0.";
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, frame, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(error, ex.Message);
    }

    [Fact]
    public async Task GivenARequestWithValidInstanceInstanceIdentifier_WhenHandlerIsExecuted_ThenNoExceptionThrown()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        await _retrieveRenderedHandler.Handle(request, CancellationToken.None);
    }

}
