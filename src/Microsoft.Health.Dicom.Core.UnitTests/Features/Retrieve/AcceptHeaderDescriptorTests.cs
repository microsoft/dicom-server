// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Web;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

public class AcceptHeaderDescriptorTests
{
    private static readonly AcceptHeaderDescriptor ValidStudyAcceptHeaderDescriptor = AcceptHeaderHandler
        .AcceptableDescriptors[ResourceType.Study]
        .First();

    [Fact]
    public void
        GivenTransferSyntaxIsNotMandatory_WhenConstructingAcceptHeaderDescriptorWithoutTransferSyntaxDefault_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => new AcceptHeaderDescriptor(
            payloadType: PayloadTypes.SinglePart,
            mediaType: KnownContentTypes.ApplicationDicom,
            isTransferSyntaxMandatory: false,
            transferSyntaxWhenMissing: string.Empty,
            acceptableTransferSyntaxes: new HashSet<string>())
        );
    }

    [Fact]
    public void
        GivenTransferSyntaxIsMandatory_WhenConstructAcceptHeaderDescriptorWithoutTransferSyntaxDefault_ShouldSucceed()
    {
        var _ = new AcceptHeaderDescriptor(
            payloadType: PayloadTypes.SinglePart,
            mediaType: KnownContentTypes.ApplicationDicom,
            isTransferSyntaxMandatory: true,
            transferSyntaxWhenMissing: string.Empty,
            acceptableTransferSyntaxes: new HashSet<string>()
        );
    }

    [Fact]
    public void GivenValidParameters_WhenUsingConstructor_ThenAllPropertiesAssigned()
    {
        PayloadTypes expectedPayloadType = PayloadTypes.MultipartRelated;
        string expectedMediaType = KnownContentTypes.ApplicationDicom;
        bool expectedIsTransferSyntaxMandatory = false;
        string expectedTransferSyntaxWhenMissing = DicomTransferSyntaxUids.ExplicitVRLittleEndian;
        ISet<string> expectedAcceptableTransferSyntaxes = new HashSet<string>() { expectedTransferSyntaxWhenMissing };

        AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
            expectedPayloadType,
            expectedMediaType,
            expectedIsTransferSyntaxMandatory,
            expectedTransferSyntaxWhenMissing,
            expectedAcceptableTransferSyntaxes
        );

        Assert.Equal(expectedPayloadType, descriptor.PayloadType);
        Assert.Equal(expectedMediaType, descriptor.MediaType);
        Assert.Equal(expectedIsTransferSyntaxMandatory, descriptor.IsTransferSyntaxMandatory);
        Assert.Equal(expectedTransferSyntaxWhenMissing, descriptor.TransferSyntaxWhenMissing);
        Assert.Equal(expectedAcceptableTransferSyntaxes, descriptor.AcceptableTransferSyntaxes);
    }


    [Fact]
    public void GivenUnsupportedMediaType_ThenIsNotAcceptable()
    {
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsAcceptable(
                new(
                    "unsupportedMediaType",
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.First())
            )
        );
    }

    [Fact]
    public void GivenUnsupportedPayloadType_ThenIsNotAcceptable()
    {
        Assert.NotEqual(PayloadTypes.None, ValidStudyAcceptHeaderDescriptor.PayloadType);
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsAcceptable(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    PayloadTypes.None,
                    ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.First())
            )
        );
    }

    [Fact]
    public void GivenAcceptHeaderWithSupportedParameters_ThenIsAcceptable()
    {
        Assert.True(ValidStudyAcceptHeaderDescriptor.IsAcceptable(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes.First())
            )
        );
    }


    [Fact]
    public void GivenUnsupportedTransferSyntax_ThenIsNotAcceptable()
    {
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsAcceptable(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    "unacceptableTransferSyntax")
            )
        );
    }

    [Fact]
    public void
        GivenAcceptHeaderWithoutTransferSyntax_WhenTransferSyntaxIsMandatoryAndNoDefaultOnDescriptor_ThenIsNotAcceptable()
    {
        AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
            payloadType: ValidStudyAcceptHeaderDescriptor.PayloadType,
            mediaType: ValidStudyAcceptHeaderDescriptor.MediaType,
            isTransferSyntaxMandatory: true,
            transferSyntaxWhenMissing: null,
            acceptableTransferSyntaxes: ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes);

        Assert.True(descriptor.IsTransferSyntaxMandatory);
        Assert.Null(descriptor.TransferSyntaxWhenMissing);

        Assert.False(descriptor.IsAcceptable(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    transferSyntax: null)
            )
        );
    }

    [Fact]
    public void
        GivenAcceptHeaderWithoutTransferSyntax_WhenTransferSyntaxNotMandatoryAndDefaultOnDescriptor_ThenIsAcceptable()
    {
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsTransferSyntaxMandatory);
        Assert.NotNull(ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing);

        Assert.True(ValidStudyAcceptHeaderDescriptor.IsAcceptable(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    transferSyntax: null)
            )
        );
    }

    [Fact]
    public void
        GivenAcceptHeaderWithoutTransferSyntax_WhenTransferSyntaxNotMandatoryAndDefaultOnDescriptor_GetTransferSyntax_ThenDefaultSyntaxReturned()
    {
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsTransferSyntaxMandatory);
        Assert.NotNull(ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing);

        Assert.Equal(
            ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing,
            ValidStudyAcceptHeaderDescriptor.GetTransferSyntax(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    transferSyntax: null)
            )
        );
    }

    [Fact]
    public void
        GivenAcceptHeaderWithTransferSyntax_WhenTransferSyntaxMandatory_GetTransferSyntax_ThenAcceptHeaderTransferSyntaxReturned()
    {
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsTransferSyntaxMandatory);
        Assert.NotNull(ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing);
        Assert.NotEqual(DicomTransferSyntaxUids.Original, ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing);

        Assert.Equal(
            DicomTransferSyntaxUids.Original,
            ValidStudyAcceptHeaderDescriptor.GetTransferSyntax(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    transferSyntax: DicomTransferSyntaxUids.Original)
            )
        );
    }

    [Fact]
    public void
        GivenAcceptHeaderWithoutTransferSyntax_WhenTransferSyntaxMandatory_GetTransferSyntax_ThenAcceptHeaderEmptyTransferSyntaxReturned()
    {
        AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
            payloadType: ValidStudyAcceptHeaderDescriptor.PayloadType,
            mediaType: ValidStudyAcceptHeaderDescriptor.MediaType,
            isTransferSyntaxMandatory: true,
            transferSyntaxWhenMissing: null,
            acceptableTransferSyntaxes: ValidStudyAcceptHeaderDescriptor.AcceptableTransferSyntaxes);

        Assert.True(descriptor.IsTransferSyntaxMandatory);
        Assert.Null(descriptor.TransferSyntaxWhenMissing);

        Assert.Null(
            descriptor.GetTransferSyntax(
                new(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    transferSyntax: null)
            ).Value
        );
    }
}
