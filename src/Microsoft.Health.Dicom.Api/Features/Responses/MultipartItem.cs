// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using EnsureThat;

namespace Microsoft.Health.Dicom.Api.Features.Responses
{
    public class MultipartItem : IDisposable
    {
        private bool _disposed = false;

        public MultipartItem(string contentType, Stream stream)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(stream, nameof(stream));

            Content = new StreamContent(stream);
            Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        public MultipartItem(string contentType, string content)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(content, nameof(content));

            Content = new StringContent(content);
            Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        public HttpContent Content { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Content.Dispose();
            }

            _disposed = true;
        }
    }
}
