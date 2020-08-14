// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class AcceptHeaderHelpers
    {
        public static IEnumerable<AcceptHeader> CreateAcceptHeadersForGetStudy(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationDicom)
        {
            return CreateAcceptHeaders(
                transferSyntax: transferSyntax,
                multipart: true,
                mediaType: mediaType);
        }

        public static IEnumerable<AcceptHeader> CreateAcceptHeadersForGetSeries(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationDicom)
        {
            return CreateAcceptHeaders(
               transferSyntax: transferSyntax,
               multipart: true,
               mediaType: mediaType);
        }

        public static IEnumerable<AcceptHeader> CreateAcceptHeadersForGetInstance(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationDicom)
        {
            return CreateAcceptHeaders(
               transferSyntax: transferSyntax,
               multipart: false,
               mediaType: mediaType);
        }

        public static IEnumerable<AcceptHeader> CreateAcceptHeadersForGetFrame(string transferSyntax = "*", string mediaType = KnownContentTypes.ApplicationOctetStream)
        {
            return CreateAcceptHeaders(
              transferSyntax: transferSyntax,
              multipart: true,
              mediaType: mediaType);
        }

        public static IEnumerable<AcceptHeader> CreateAcceptHeaders(string transferSyntax = "*", bool multipart = true, string mediaType = KnownContentTypes.ApplicationOctetStream, double? quantity = null)
        {
            return new AcceptHeader[] { new AcceptHeader(mediaType, multipart, transferSyntax, quantity) };
        }
    }
}
