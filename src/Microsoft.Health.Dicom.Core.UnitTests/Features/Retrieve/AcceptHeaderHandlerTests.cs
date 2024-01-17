// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

public class AcceptHeaderHandlerTests
{
    private readonly AcceptHeaderHandler _handler;

    private static readonly AcceptHeaderDescriptor ValidStudyAcceptHeaderDescriptor = AcceptHeaderHandler
        .AcceptableDescriptors[ResourceType.Study]
        .First();

    public static IEnumerable<object[]> UnacceptableHeadersList()
    {
        yield return new object[]
        {
            new List<AcceptHeader>
            {
                new(
                    "unsupportedMediaType",
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing)
            },
            ResourceType.Study
        };
        yield return new object[]
        {
            new List<AcceptHeader>
            {
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    "unsupportedTransferSyntax")
            },
            ResourceType.Study
        };
        yield return new object[]
        {
            new List<AcceptHeader>
            {
                new(
                    "unsupportedMediaType",
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing),
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    "unsupportedTransferSyntax")
            },
            ResourceType.Study
        };
    }

    public static IEnumerable<object[]> AnyMediaTypeHeadersList()
    {
        yield return new object[]
        {
            new List<AcceptHeader>
            {
                new(
                    "*/*",
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing)
            },
            ResourceType.Study,
            KnownContentTypes.ApplicationDicom,
            PayloadTypes.MultipartRelated,
        };
        yield return new object[]
        {
            new List<AcceptHeader>
            {
                new(
                    "*/*",
                    ValidStudyAcceptHeaderDescriptor.PayloadType)
            },
            ResourceType.Series,
            KnownContentTypes.ApplicationDicom,
            PayloadTypes.MultipartRelated,
        };
        yield return new object[]
        {
            new List<AcceptHeader>
            {
                new(
                    "*/*",
                    ValidStudyAcceptHeaderDescriptor.PayloadType)
            },
            ResourceType.Frames,
            KnownContentTypes.ApplicationOctetStream,
            PayloadTypes.MultipartRelated,
        };
    }

    public AcceptHeaderHandlerTests()
    {
        _handler = new AcceptHeaderHandler(NullLogger<AcceptHeaderHandler>.Instance);
    }

    [Fact]
    public void
        GivenASingleRequestedAcceptHeader_WhenRequestedMatchesHeadersWeAccept_ThenShouldReturnAcceptedHeaderWithTransferSyntaxAndDescriptorThatMatched()
    {
        AcceptHeader requestedAcceptHeader = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing
        );

        AcceptHeader matchedAcceptHeader = _handler.GetValidAcceptHeader(
            ResourceType.Study,
            new List<AcceptHeader>() { requestedAcceptHeader }
        );

        Assert.Equivalent(requestedAcceptHeader, matchedAcceptHeader, strict: true);
    }

    [Theory]
    [MemberData(nameof(UnacceptableHeadersList))]
    public void GivenNoMatchedAcceptHeaders_WhenGetTransferSyntax_ThenShouldThrowNotAcceptableException(
        List<AcceptHeader> requestedAcceptHeaders,
        ResourceType requestedResourceType)
    {
        Assert.ThrowsAny<NotAcceptableException>(() => _handler.GetValidAcceptHeader(
            requestedResourceType,
            requestedAcceptHeaders
        ));
    }

    [Fact]
    public void
        GivenMultipleMatchedAcceptHeadersWithDifferentQuality_WhenHeadersRequestedAreAllSupported_ThenShouldReturnHighestQuality()
    {
        Assert.True(ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.Count > 1);

        AcceptHeader requestedAcceptHeader1 = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.First(),
            quality: 0.5
        );

        AcceptHeader requestedAcceptHeader2 = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.Last(),
            quality: 0.9
        );

        AcceptHeader matchedAcceptHeader = _handler.GetValidAcceptHeader(
            ResourceType.Study,
            new[] { requestedAcceptHeader1, requestedAcceptHeader2 }
        );

        Assert.Equivalent(requestedAcceptHeader2, matchedAcceptHeader, strict: true);
    }

    [Fact]
    public void
        GivenMultipleMatchedAcceptHeadersWithDifferentQuality_WhenTransferSyntaxRequestedOfHigherQualityNotSupported_ThenShouldReturnNextHighestQuality()
    {
        // When we multiple headers requested, but the one with highest quality "preference"
        // is requested with a TransferSyntax that we do not support,
        // we should use the next highest quality requested with a supported TransferSyntax.

        Assert.True(ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.Count > 1);

        AcceptHeader requestedAcceptHeader1 = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            DicomTransferSyntaxUids.Original,
            quality: 0.3
        );

        AcceptHeader requestedAcceptHeader2 = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.Last(),
            quality: 0.5
        );

        AcceptHeader requestedAcceptHeader3 = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            "unsupportedTransferSyntax",
            quality: 0.7
        );

        AcceptHeader matchedAcceptHeader = _handler.GetValidAcceptHeader(
            ResourceType.Study,
            new[] { requestedAcceptHeader1, requestedAcceptHeader2, requestedAcceptHeader3 }
        );

        Assert.Equivalent(requestedAcceptHeader2, matchedAcceptHeader, strict: true);
    }

    [Fact]
    public void GivenMultipleMatchedAcceptHeaderNoQuality_WhenTransferSyntaxRequested_ThenShouldReturnOriginal()
    {
        AcceptHeader requestedAcceptHeader1 = new AcceptHeader(
                payloadType: PayloadTypes.MultipartRelated,
                mediaType: KnownContentTypes.ImageJpeg2000,
                transferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID);

        AcceptHeader requestedAcceptHeader2 = new AcceptHeader(
                payloadType: PayloadTypes.SinglePart,
                mediaType: KnownContentTypes.ApplicationOctetStream,
                transferSyntax: DicomTransferSyntaxUids.Original);


        AcceptHeader matchedAcceptHeader = _handler.GetValidAcceptHeader(
            ResourceType.Frames,
            new[] { requestedAcceptHeader1, requestedAcceptHeader2 }
        );

        Assert.Equivalent(requestedAcceptHeader2, matchedAcceptHeader, strict: true);
    }

    [Theory]
    [MemberData(nameof(AnyMediaTypeHeadersList))]
    public void
        GivenASingleRequestedAcceptHeaderWithAnyMediaType_WhenRequestedMatchesHeadersWeAccept_ThenShouldReturnAcceptedHeaderWithTransferSyntaxAndDescriptorThatMatched(
            List<AcceptHeader> requestedAcceptHeaders,
            ResourceType requestedResourceType,
            string mediaType,
            PayloadTypes payloadType)
    {
        AcceptHeader matchedAcceptHeader = _handler.GetValidAcceptHeader(
            requestedResourceType,
            requestedAcceptHeaders
        );

        var expectedTransferSyntax = string.IsNullOrEmpty(requestedAcceptHeaders.First().TransferSyntax.Value) ?
            "*" :
            requestedAcceptHeaders.First().TransferSyntax.Value;

        Assert.Equal(mediaType, matchedAcceptHeader.MediaType);
        Assert.Equal(payloadType, matchedAcceptHeader.PayloadType);
        Assert.Equal(expectedTransferSyntax, matchedAcceptHeader.TransferSyntax);
    }
}
