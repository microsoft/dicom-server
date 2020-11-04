// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client
{
    public class DicomWebResponse : IDisposable
    {
        private readonly HttpResponseMessage _response;
        private bool _disposed;

        public DicomWebResponse(HttpResponseMessage response)
        {
            EnsureArg.IsNotNull(response, nameof(response));

            _response = response;
        }

        public HttpStatusCode StatusCode => _response.StatusCode;

        public HttpResponseHeaders ResponseHeaders => _response.Headers;

        public HttpContentHeaders ContentHeaders => _response.Content?.Headers;

        protected HttpContent Content => _response.Content;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _response.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
