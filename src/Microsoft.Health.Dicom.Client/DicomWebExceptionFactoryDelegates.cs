// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Client
{
    public delegate DicomWebException DefaultDicomWebExceptionFactory(HttpStatusCode statusCode, HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders, string responseBody);

    public delegate DicomWebException DicomWebExceptionFactory(DefaultDicomWebExceptionFactory defaultExceptionFactory, HttpStatusCode statusCode, HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders, string responseBody);
}
