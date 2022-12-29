// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;

public class AcceptHeaderDescriptorsTests
{
    private static readonly AcceptHeaderDescriptor ValidStudyAcceptHeaderDescriptor = RetrieveTransferSyntaxHandler
        .AcceptableDescriptors[ResourceType.Study]
        .Descriptors
        .First();

    [Fact]
    public void GivenDescriptorsIsNotNull_WhenConstructAcceptHeaderDescriptors_ThenShouldSucceed()
    {
        AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader();
        AcceptHeaderDescriptor descriptor = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: true);
        AcceptHeaderDescriptors descriptors = new AcceptHeaderDescriptors(descriptor);
        Assert.Single(descriptors.Descriptors);
        Assert.Same(descriptor, descriptors.Descriptors.First());
    }

    [Fact]
    public void GivenAcceptHeaders_WhenSeveralMatchAndOthersNot_ThenHeaderIsValid()
    {
        AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader();
        AcceptHeaderDescriptor matchDescriptor1 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: true);
        AcceptHeaderDescriptor matchDescriptor2 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: true);
        AcceptHeaderDescriptor notMatchDescriptor = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: false);
        AcceptHeaderDescriptors acceptHeaderDescriptors = new AcceptHeaderDescriptors(matchDescriptor1, matchDescriptor2, notMatchDescriptor);

        Assert.True(acceptHeaderDescriptors.IsValidAcceptHeader(acceptHeader));
    }

    [Fact]
    public void GivenAcceptHeaders_WhenNoMatch_ThenHeaderIsNotValid()
    {
        AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader();
        AcceptHeaderDescriptor notMatchDescriptor1 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: false);
        AcceptHeaderDescriptor notMatchDescriptor2 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: false);
        AcceptHeaderDescriptors acceptHeaderDescriptors = new AcceptHeaderDescriptors(notMatchDescriptor1, notMatchDescriptor2);

        Assert.False(acceptHeaderDescriptors.IsValidAcceptHeader(acceptHeader));
    }

    public static IEnumerable<object[]> UnacceptableStudyHeadersList()
    {
        yield return new object[]
        {
            new AcceptHeader(
                    "unsupportedMediaType",
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing)
        };
        yield return new object[]
        {
            new AcceptHeader(
                    ValidStudyAcceptHeaderDescriptor.MediaType,
                    ValidStudyAcceptHeaderDescriptor.PayloadType,
                    "unsupportedTransferSyntax")
        };
    }

    [Theory]
    [MemberData(nameof(UnacceptableStudyHeadersList))]
    public void GivenInvalidAcceptHeader_ThenIsNotAcceptable(
        AcceptHeader requestedAcceptHeader)
    {
        Assert.False(ValidStudyAcceptHeaderDescriptor.IsAcceptable(requestedAcceptHeader));
    }

    [Fact]
    public void GivenValidAcceptHeader_ThenIsAcceptable()
    {
        AcceptHeader acceptHeader = new AcceptHeader(
            ValidStudyAcceptHeaderDescriptor.MediaType,
            ValidStudyAcceptHeaderDescriptor.PayloadType,
            ValidStudyAcceptHeaderDescriptor.TransferSyntaxWhenMissing);
        Assert.True(ValidStudyAcceptHeaderDescriptor.IsAcceptable(acceptHeader));
    }

}
