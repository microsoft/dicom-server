// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http.Headers;
using Dicom;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebException : Exception
    {
        public DicomWebException(
            HttpStatusCode statusCode,
            HttpResponseHeaders responseHeaders,
            HttpContentHeaders contentHeaders,
            string responseMessage)
            : this(statusCode, responseHeaders, contentHeaders)
        {
            ResponseMessage = responseMessage;
        }

        public DicomWebException(
            HttpStatusCode statusCode,
            HttpResponseHeaders responseHeaders,
            HttpContentHeaders contentHeaders,
            DicomDataset responseDataset)
            : this(statusCode, responseHeaders, contentHeaders)
        {
            ResponseDataset = responseDataset;
        }

        private DicomWebException(
            HttpStatusCode statusCode,
            HttpResponseHeaders responseHeaders,
            HttpContentHeaders contentHeaders)
        {
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            ContentHeaders = contentHeaders;
        }

        public HttpStatusCode StatusCode { get; }

        public HttpResponseHeaders ResponseHeaders { get; }

        public HttpContentHeaders ContentHeaders { get; }

        public string ResponseMessage { get; }

        public DicomDataset ResponseDataset { get; }

        public override string Message
        {
            get
            {
                if (!string.IsNullOrEmpty(ResponseMessage))
                {
                    return $"{StatusCode}: {ResponseMessage}";
                }

                return StatusCode.ToString();
            }
        }
    }
}
