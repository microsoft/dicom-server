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

    [Fact(Skip = "Will be enabled later as https://microsofthealth.visualstudio.com/Health/_workitems/edit/75782")]
    public void GivenMultipleMatchedAcceptHeadersWithDifferentQuality_WhenGetTransferSyntax_ThenShouldReturnLargestQuality()
    {
        string expectedTransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID;
        AcceptHeader acceptHeader1 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, transferSyntax: DicomTransferSyntaxUids.Original);
        AcceptHeader acceptHeader2 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.9, transferSyntax: expectedTransferSyntax);
        AcceptHeaderDescriptor acceptHeaderDescriptor;
        string transferSyntax = _handler.GetTransferSyntax(ResourceType.Frames, new[] { acceptHeader1, acceptHeader2 }, out acceptHeaderDescriptor, out AcceptHeader acceptHeader);
        Assert.Equal(expectedTransferSyntax, transferSyntax);
        Assert.Equal(acceptHeader2.MediaType, acceptHeaderDescriptor.MediaType);
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
    public void GivenMultipleAcceptHeaders_WhenGetTransferSyntax_ThenShouldThrowNotAcceptableException()
    {
        AcceptHeader acceptHeader1 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, transferSyntax: DicomTransferSyntaxUids.Original);
        AcceptHeader acceptHeader2 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.9, transferSyntax: DicomTransferSyntaxUids.Original);
        AcceptHeaderDescriptor acceptHeaderDescriptor;
        Assert.ThrowsAny<NotAcceptableException>(() => _handler.GetTransferSyntax(ResourceType.Study, new[] { acceptHeader1, acceptHeader2 }, out acceptHeaderDescriptor, out AcceptHeader acceptHeader));
    }

    [Fact]
    public void GivenMultipleAcceptHeaders_WhenGetTransferSyntax_ThenShouldNotThrowException()
    {
        // As standard, since we support both image/jp2 and application/octet-stream, and image/jp2 has higher Q, we should choose image/jp2 ,
        // but we don’t support RLELossless, so we should return NotAcceptable.

        // multipart/related;type="image/jls",multipart/related;type="image/jpeg"
        // multipart/related;type=”image/jp2”;transfer-syntax=1.2.840.10008.1.2.5(RLELossless);q=0.7,multipart/related;type=”application/octet-stream”;transfer-syntax= 1.2.840.10008.1.2.1 (ExplicitVRLittleEndian);q=0.5

        var acceptHeader1 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, mediaType: KnownContentTypes.ApplicationOctetStream, transferSyntax: DicomTransferSyntaxUids.Original);
        var acceptHeader2 = AcceptHeaderHelpers.CreateAcceptHeaderForGetSeries(quality: 0.7, mediaType: KnownContentTypes.ImageJpeg2000, transferSyntax: DicomTransferSyntaxUids.Original);
        var acceptHeader3 = AcceptHeaderHelpers.CreateAcceptHeaderForGetFrame(quality: 0.5, mediaType: KnownContentTypes.ImageJpegLs, transferSyntax: DicomTransferSyntaxUids.Original);

        AcceptHeaderDescriptor acceptHeaderDescriptor;
        var transferSyntax = _handler
            .GetTransferSyntax(ResourceType.Study, new[]
                {
                    acceptHeader1,
                    acceptHeader2,
                    acceptHeader3
                }, out acceptHeaderDescriptor, out AcceptHeader acceptHeader);

        Assert.False(string.IsNullOrWhiteSpace(transferSyntax));
    }
}
