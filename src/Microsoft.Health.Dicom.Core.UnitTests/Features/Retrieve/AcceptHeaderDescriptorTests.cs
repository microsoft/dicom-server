// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class AcceptHeaderDescriptorTests
    {
        [Fact]
        public void GivenTransferSyntaxIsNotMandatory_WhenConstructAcceptHeaderDescriptorWithoutTransferSyntax_ShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => CreateAcceptHeaderDescriptor(isTransferSyntaxMandatory: false, transferSyntaxWhenMissing: string.Empty, acceptableTransferSyntaxes: new HashSet<string>()));
        }

        [Fact]
        public void GivenTransferSyntaxIsMandatory_WhenConstructAcceptHeaderDescriptorWithoutTransferSyntax_ShouldSucceed()
        {
            CreateAcceptHeaderDescriptor(isTransferSyntaxMandatory: true, transferSyntaxWhenMissing: string.Empty, acceptableTransferSyntaxes: new HashSet<string>());
        }

        [Fact]
        public void GivenValidInputs_WhenContructAcceptHeaderDescriptor_ShouldSucceed()
        {
            PayloadTypes payloadType = PayloadTypes.MultipartRelated;
            string mediaType = KnownContentTypes.ApplicationDicom;
            bool isTransferSyntaxMandatory = false;
            string transferSyntaxWhenMissing = DicomTransferSyntaxUids.ExplicitVRLittleEndian;
            ISet<string> acceptableTransferSyntaxes = new HashSet<string>() { transferSyntaxWhenMissing };
            AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(payloadType, mediaType, isTransferSyntaxMandatory, transferSyntaxWhenMissing, acceptableTransferSyntaxes);
            Assert.Equal(payloadType, descriptor.PayloadType);
            Assert.Equal(mediaType, descriptor.MediaType);
            Assert.Equal(isTransferSyntaxMandatory, descriptor.IsTransferSyntaxMandatory);
            Assert.Equal(transferSyntaxWhenMissing, descriptor.TransferSyntaxWhenMissing);
            Assert.Equal(acceptableTransferSyntaxes, descriptor.AcceptableTransferSyntaxes);
        }

        [Theory]
        [InlineData(PayloadTypes.SinglePart, PayloadTypes.SinglePart, true)]
        [InlineData(PayloadTypes.SinglePart, PayloadTypes.MultipartRelated, false)]
        [InlineData(PayloadTypes.MultipartRelated, PayloadTypes.MultipartRelated, true)]
        [InlineData(PayloadTypes.MultipartRelated, PayloadTypes.SinglePart, false)]
        [InlineData(PayloadTypes.SinglePartOrMultipartRelated, PayloadTypes.SinglePart, true)]
        [InlineData(PayloadTypes.SinglePartOrMultipartRelated, PayloadTypes.MultipartRelated, true)]
        public void GivenPayloadType_WhenCheckIsAcceptable_ShouldSucceed(PayloadTypes descriptorPayloadType, PayloadTypes acceptHeaderPayloadType, bool isAcceptable)
        {
            (AcceptHeader, AcceptHeaderDescriptor) testData = CreateAcceptHeaderAndDescriptorForPayloadType(descriptorPayloadType, acceptHeaderPayloadType);
            string transferSyntax;
            AcceptHeader acceptHeader = testData.Item1;
            AcceptHeaderDescriptor descriptor = testData.Item2;
            Assert.Equal(isAcceptable, descriptor.IsAcceptable(acceptHeader, out transferSyntax));
        }

        [Theory]
        [InlineData(KnownContentTypes.ApplicationDicom, "Application/Dicom", true)]
        [InlineData(KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationDicomJson, false)]
        public void GivenMediaType_WhenCheckIsAcceptable_ShouldSucceed(string descriptorMediaType, string acceptHeaderMediaType, bool isAcceptable)
        {
            (AcceptHeader, AcceptHeaderDescriptor) testData = CreateAcceptHeaderAndDescriptorForMediaType(descriptorMediaType, acceptHeaderMediaType);
            string transferSyntax;
            AcceptHeader acceptHeader = testData.Item1;
            AcceptHeaderDescriptor descriptor = testData.Item2;
            Assert.Equal(isAcceptable, descriptor.IsAcceptable(acceptHeader, out transferSyntax));
        }

        [Theory]
        [InlineData(true, "", "", false, "")]
        [InlineData(true, "", KnownContentTypes.ApplicationDicom, true, KnownContentTypes.ApplicationDicom)]
        [InlineData(false, KnownContentTypes.ApplicationDicom, "", true, KnownContentTypes.ApplicationDicom)]
        [InlineData(false, KnownContentTypes.ApplicationDicom, KnownContentTypes.ApplicationDicomJson, true, KnownContentTypes.ApplicationDicomJson)]
        public void GivenTransferSyntaxMandatory_WhenCheckIsAcceptable_ShouldSucceed(bool isTransferSyntaxMandatory, string transferSyntaxWhenMissing, string acceptHeaderTransferSyntax, bool isAcceptable, string expectedTransferSyntax)
        {
            (AcceptHeader, AcceptHeaderDescriptor) testData = CreateAcceptHeaderAndDescriptorForTransferSyntaxMandatory(isTransferSyntaxMandatory, transferSyntaxWhenMissing, acceptHeaderTransferSyntax);
            string transferSyntax;
            AcceptHeader acceptHeader = testData.Item1;
            AcceptHeaderDescriptor descriptor = testData.Item2;
            Assert.Equal(isAcceptable, descriptor.IsAcceptable(acceptHeader, out transferSyntax));
            if (isAcceptable)
            {
                Assert.Equal(expectedTransferSyntax, transferSyntax);
            }
        }

        [Theory]
        [InlineData(true, KnownContentTypes.ApplicationDicom, true, KnownContentTypes.ApplicationDicom)]
        [InlineData(false, KnownContentTypes.ApplicationDicom, false, "")]
        public void GivenAcceptableTransferSyntaxes_WhenCheckIsAcceptable_ShouldSucceed(bool inSet, string acceptHeaderTransferSyntax, bool isAcceptable, string expectedTransferSyntax)
        {
            (AcceptHeader, AcceptHeaderDescriptor) testData = CreateAcceptHeaderAndDescriptorForAcceptableSet(inSet, acceptHeaderTransferSyntax);
            string transferSyntax;
            AcceptHeader acceptHeader = testData.Item1;
            AcceptHeaderDescriptor descriptor = testData.Item2;
            Assert.Equal(isAcceptable, descriptor.IsAcceptable(acceptHeader, out transferSyntax));
            if (isAcceptable)
            {
                Assert.Equal(expectedTransferSyntax, transferSyntax);
            }
        }

        private (AcceptHeader, AcceptHeaderDescriptor) CreateAcceptHeaderAndDescriptorForAcceptableSet(bool inSet, string acceptHeaderTransferSyntax)
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader(transferSyntax: acceptHeaderTransferSyntax);
            AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
               payloadType: acceptHeader.PayloadType,
               mediaType: acceptHeader.MediaType.Value,
               isTransferSyntaxMandatory: true,
               transferSyntaxWhenMissing: string.Empty,
               acceptableTransferSyntaxes: inSet ? new HashSet<string>() { acceptHeader.TransferSyntax.Value } : new HashSet<string>() { });
            return (acceptHeader, descriptor);
        }

        private (AcceptHeader, AcceptHeaderDescriptor) CreateAcceptHeaderAndDescriptorForTransferSyntaxMandatory(bool isTransferSyntaxMandatory, string transferSyntaxWhenMissing, string acceptHeaderTransferSyntax)
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader(transferSyntax: acceptHeaderTransferSyntax);
            AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
               payloadType: acceptHeader.PayloadType,
               mediaType: acceptHeader.MediaType.Value,
               isTransferSyntaxMandatory: isTransferSyntaxMandatory,
               transferSyntaxWhenMissing: transferSyntaxWhenMissing,
               acceptableTransferSyntaxes: new HashSet<string>() { acceptHeader.TransferSyntax.Value });
            return (acceptHeader, descriptor);
        }

        private (AcceptHeader, AcceptHeaderDescriptor) CreateAcceptHeaderAndDescriptorForMediaType(string descriptorMediaType, string acceptHeaderMediaType)
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader(mediaType: acceptHeaderMediaType);
            AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
               payloadType: acceptHeader.PayloadType,
               mediaType: descriptorMediaType,
               isTransferSyntaxMandatory: true,
               transferSyntaxWhenMissing: string.Empty,
               acceptableTransferSyntaxes: new HashSet<string>() { acceptHeader.TransferSyntax.Value });
            return (acceptHeader, descriptor);
        }

        private (AcceptHeader, AcceptHeaderDescriptor) CreateAcceptHeaderAndDescriptorForPayloadType(PayloadTypes descriptorPayloadType, PayloadTypes acceptHeaderPayloadType)
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader(payloadType: acceptHeaderPayloadType);
            AcceptHeaderDescriptor descriptor = new AcceptHeaderDescriptor(
               payloadType: descriptorPayloadType,
               mediaType: acceptHeader.MediaType.Value,
               isTransferSyntaxMandatory: true,
               transferSyntaxWhenMissing: string.Empty,
               acceptableTransferSyntaxes: new HashSet<string>() { acceptHeader.TransferSyntax.Value });
            return (acceptHeader, descriptor);
        }

        private AcceptHeaderDescriptor CreateAcceptHeaderDescriptor(
            PayloadTypes payloadType = PayloadTypes.SinglePart,
            string mediaType = KnownContentTypes.ApplicationDicom,
            bool isTransferSyntaxMandatory = true,
            string transferSyntaxWhenMissing = "",
            ISet<string> acceptableTransferSyntaxes = null)
        {
            return new AcceptHeaderDescriptor(payloadType, mediaType, isTransferSyntaxMandatory, transferSyntaxWhenMissing, acceptableTransferSyntaxes);
        }
    }
}
