// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class AcceptHeaderHelpers
    {
        public static AcceptHeader CreateAcceptHeaderForGetStudy(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationDicom, double? quality = null)
        {
            return CreateAcceptHeader(
                transferSyntax: transferSyntax,
                payloadType: PayloadTypes.MultipartRelated,
                mediaType: mediaType,
                quality: quality);
        }

        public static AcceptHeader CreateAcceptHeaderForGetSeries(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationDicom, double? quality = null)
        {
            return CreateAcceptHeader(
               transferSyntax: transferSyntax,
               payloadType: PayloadTypes.MultipartRelated,
               mediaType: mediaType,
               quality: quality);
        }

        public static AcceptHeader CreateAcceptHeaderForGetInstance(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationDicom, double? quality = null, PayloadTypes payloadTypes = PayloadTypes.SinglePart)
        {
            return CreateAcceptHeader(
               transferSyntax: transferSyntax,
               payloadType: payloadTypes,
               mediaType: mediaType,
               quality: quality);
        }

        public static AcceptHeader CreateAcceptHeaderForGetFrame(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationOctetStream, double? quality = null)
        {
            return CreateAcceptHeader(
              transferSyntax: transferSyntax,
              payloadType: PayloadTypes.MultipartRelated,
              mediaType: mediaType,
              quality: quality);
        }

        public static AcceptHeader CreateAcceptHeader(string transferSyntax = "*", PayloadTypes payloadType = PayloadTypes.MultipartRelated, string mediaType = KnownContentTypes.ApplicationOctetStream, double? quality = null)
        {
            return new AcceptHeader(mediaType, payloadType, transferSyntax, quality);
        }
    }
}
