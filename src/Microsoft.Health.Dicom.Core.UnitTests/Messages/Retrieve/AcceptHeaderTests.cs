// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class AcceptHeaderTests
    {
        [Fact]
        public void GivenValidInput_WhenConstructAcceptHeader_ThenShouldSucceed()
        {
            StringSegment mediaType = KnownContentTypes.ApplicationDicom;
            PayloadTypes payloadType = PayloadTypes.MultipartRelated;
            StringSegment transferSytnax = DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID;
            double quality = 0.5;
            AcceptHeader header = new AcceptHeader(mediaType, payloadType, transferSytnax, quality);

            Assert.Equal(mediaType, header.MediaType);
            Assert.Equal(payloadType, header.PayloadType);
            Assert.Equal(transferSytnax, header.TransferSyntax);
            Assert.Equal(quality, header.Quality);
        }

        [Fact]
        public void GivenValidInput_WhenToString_ThenShouldReturnExpectedContent()
        {
            StringSegment mediaType = KnownContentTypes.ApplicationDicom;
            PayloadTypes payloadType = PayloadTypes.MultipartRelated;
            StringSegment transferSytnax = DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID;
            double quality = 0.5;
            AcceptHeader header = new AcceptHeader(mediaType, payloadType, transferSytnax, quality);

            Assert.Equal($"MediaType:'{mediaType}', PayloadType:'{payloadType}', TransferSyntax:'{transferSytnax}', Quality:'{quality}'", header.ToString());
        }
    }
}
