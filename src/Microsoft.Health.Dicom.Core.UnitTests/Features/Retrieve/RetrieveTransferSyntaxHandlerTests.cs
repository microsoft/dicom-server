// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

public class RetrieveTransferSyntaxHandlerTests
{
    private readonly RetrieveTransferSyntaxHandler _handler;

    public RetrieveTransferSyntaxHandlerTests()
    {
        _handler = new RetrieveTransferSyntaxHandler(NullLogger<RetrieveTransferSyntaxHandler>.Instance);
    }

    [Fact]
    public void GivenNoMatchedAcceptHeaders_WhenGetTransferSyntax_ThenShouldThrowNotAcceptableException()
    {
        // Use content type that GetStudy doesn't support
        AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy(mediaType: KnownContentTypes.ImageJpeg);
        AcceptHeaderDescriptor acceptHeaderDescriptor;
        Assert.ThrowsAny<NotAcceptableException>(() => _handler.GetTransferSyntax(ResourceType.Study, new[] { acceptHeader }, out acceptHeaderDescriptor, out AcceptHeader acceptedHeader));
    }

    [Fact]
    public void GivenMultipleAcceptHeaders_WhenGetTransferSyntax_ThenShouldNotThrowNotAcceptableException()
    {
        AcceptHeader acceptHeader1 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, transferSyntax: DicomTransferSyntaxUids.Original);
        AcceptHeader acceptHeader2 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.9, transferSyntax: DicomTransferSyntaxUids.Original);
        AcceptHeaderDescriptor acceptHeaderDescriptor;
        string transferSyntax = _handler.GetTransferSyntax(ResourceType.Frames, new[] { acceptHeader1, acceptHeader2 }, out acceptHeaderDescriptor, out AcceptHeader acceptHeader);
        Assert.NotEmpty(transferSyntax);
    }

    [Fact]
    public void GivenMultipleMatchedAcceptHeadersWithDifferentQuality_WhenBothTransferSyntaxRequestedSupported_ThenShouldReturnLargestQuality()
    {
        AcceptHeader acceptHeader1 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, transferSyntax: DicomTransferSyntaxUids.Original);
        AcceptHeader acceptHeader2 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.9, transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);
        AcceptHeaderDescriptor acceptHeaderDescriptor;
        string transferSyntax = _handler.GetTransferSyntax(ResourceType.Frames, new[] { acceptHeader1, acceptHeader2 }, out acceptHeaderDescriptor, out AcceptHeader acceptHeader);
        Assert.Equal(acceptHeader2.TransferSyntax, transferSyntax);
        Assert.Equal(acceptHeader2.Quality, acceptHeader.Quality);
    }

    [Fact]
    public void GivenMultipleMatchedAcceptHeadersWithDifferentQuality_WhenTransferSyntaxRequestedOfHigherQualityNotSupported_ThenShouldReturnNextLargestQuality()
    {
        // When we multiple headers requested, but the one with highest quality "preference"
        // is requested with a TransferSyntax that we do not support,
        // we should use the next highest quality requested with a supported TransferSyntax.

        var acceptHeader1 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, transferSyntax: DicomTransferSyntaxUids.Original);
        const string rleLosslessTransferSyntax = "1.2.840.10008.1.2.5";
        var acceptHeader2 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.7, transferSyntax: rleLosslessTransferSyntax);
        var acceptHeader3 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.3, transferSyntax: DicomTransferSyntaxUids.Original);

        AcceptHeaderDescriptor acceptHeaderDescriptor;
        var transferSyntax = _handler
            .GetTransferSyntax(ResourceType.Frames, new[]
                {
                    acceptHeader1,
                    acceptHeader2,
                    acceptHeader3
                }, out acceptHeaderDescriptor, out AcceptHeader acceptHeader);

        Assert.Equal(acceptHeader1.TransferSyntax, transferSyntax);
        Assert.Equal(acceptHeader1.Quality, acceptHeader.Quality);
    }
}
