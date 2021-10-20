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
        private const string MultipartTransferSyntax = "transfer-syntax";
        private bool _disposed;
        private readonly StreamContent _streamContent;

        public MultipartItem(string contentType, Stream stream, string transferSyntax = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(stream, nameof(stream));

            _streamContent = new StreamContent(stream);
            var mediaType = new MediaTypeHeaderValue(contentType);

            if (!string.IsNullOrWhiteSpace(transferSyntax))
                mediaType.Parameters.Add(new NameValueHeaderValue(MultipartTransferSyntax, transferSyntax));

            _streamContent.Headers.ContentType = mediaType;
        }

        public HttpContent Content => _streamContent;

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
                _streamContent.Dispose();
            }

            _disposed = true;
        }
    }
}
