// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common
{
    public class DicomWebResponse
    {
        private HttpResponseMessage _response;

        public DicomWebResponse(HttpResponseMessage response)
        {
            EnsureArg.IsNotNull(response, nameof(response));

            _response = response;
        }

        public HttpStatusCode StatusCode => _response.StatusCode;

        public HttpResponseHeaders Headers => _response.Headers;

        public HttpContent Content => _response.Content;
    }
}
