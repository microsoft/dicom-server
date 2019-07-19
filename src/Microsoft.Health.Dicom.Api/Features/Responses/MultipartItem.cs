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
    public class MultipartItem
    {
        public MultipartItem(string contentType, Stream stream)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(stream, nameof(stream));

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            Content = streamContent;
            Disposable = stream;
        }

        public HttpContent Content { get; }

        public IDisposable Disposable { get; }
    }
}
