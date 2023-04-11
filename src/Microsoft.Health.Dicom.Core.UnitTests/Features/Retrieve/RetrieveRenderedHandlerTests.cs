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

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
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

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
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

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
        var ex = await Assert.ThrowsAsync<InvalidIdentifierException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(ValidationErrorCode.UidIsInvalid, ex.ErrorCode);
    }

    [Fact]
    public async Task GivenARequestWithValidInstanceInstanceIdentifier_WhenHandlerIsExecuted_ThenNoExceptionThrown()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
        await _retrieveRenderedHandler.Handle(request, CancellationToken.None);
    }


    [Fact]
    public async Task GivenARequestWithMultipleAcceptHeaders_WhenHandlerIsExecuted_ThenNotAcceptableExceptionExceptionIsThrown()
    {
        const string expectedErrorMessage = "The request contains multiple accept headers, which is not supported.";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader(), AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
        var ex = await Assert.ThrowsAsync<NotAcceptableException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task GivenARequestWithInvalidAcceptHeader_WhenHandlerIsExecuted_ThenNotAcceptableExceptionExceptionIsThrown()
    {
        const string expectedErrorMessage = "The request headers are not acceptable";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() });
        var ex = await Assert.ThrowsAsync<NotAcceptableException>(() => _retrieveRenderedHandler.Handle(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }
}
