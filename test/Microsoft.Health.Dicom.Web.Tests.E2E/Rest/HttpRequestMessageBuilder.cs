// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class HttpRequestMessageBuilder
    {
        private const string TransferSyntaxHeaderName = "transfer-syntax";
        private const string MultipartRelatedContentType = "multipart/related";

        public HttpRequestMessage Build(Uri requestUri, bool singlePart, string mediaType, string dicomTransferSyntax)
        {
            MediaTypeWithQualityHeaderValue headerValue = singlePart ? BuildSinglePartMediaTypeHeader(mediaType) : BuildMultipartMediaTypeHeader(mediaType);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("Accept", BuildAcceptHeader(headerValue, dicomTransferSyntax));
            return request;
        }

        private MediaTypeWithQualityHeaderValue BuildMultipartMediaTypeHeader(string mediaType)
        {
            var multipartHeader = new MediaTypeWithQualityHeaderValue(MultipartRelatedContentType);
            var contentHeader = new NameValueHeaderValue("type", "\"" + mediaType + "\"");
            multipartHeader.Parameters.Add(contentHeader);
            return multipartHeader;
        }

        private MediaTypeWithQualityHeaderValue BuildSinglePartMediaTypeHeader(string mediaType)
        {
            return new MediaTypeWithQualityHeaderValue(mediaType);
        }

        private string BuildAcceptHeader(MediaTypeWithQualityHeaderValue mediaTypeHeader, string dicomTransferSyntax)
        {
            string transferSyntaxHeader = dicomTransferSyntax == null ? string.Empty : $";{TransferSyntaxHeaderName}=\"{dicomTransferSyntax}\"";

            return $"{mediaTypeHeader}{transferSyntaxHeader}";
        }
    }
}
