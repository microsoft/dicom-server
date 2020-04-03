// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Multipart reader implemented by using AspNetCore's <see cref="MultipartReader"/>.
    /// </summary>
    internal class AspNetCoreMultipartReader : IMultipartReader
    {
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly MultipartReader _multipartReader;

        internal AspNetCoreMultipartReader(string contentType, Stream body, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(contentType, nameof(contentType));
            EnsureArg.IsNotNull(body, nameof(body));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;

            if (!MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue media))
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomApiResource.UnsupportedContentType, contentType));
            }

            var isMultipartRelated = media.MediaType.Equals(KnownContentTypes.MultipartRelated, StringComparison.InvariantCultureIgnoreCase);
            string boundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();

            if (!isMultipartRelated || string.IsNullOrWhiteSpace(boundary))
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomApiResource.InvalidMultipartContentType, contentType));
            }

            _multipartReader = new MultipartReader(boundary, body);
        }

        /// <inheritdoc />
        public async Task<MultipartBodyPart> ReadNextBodyPartAsync(CancellationToken cancellationToken)
        {
            MultipartSection section = await _multipartReader.ReadNextSectionAsync(cancellationToken);

            if (section == null)
            {
                return null;
            }

            // The stream must be consumed before the next ReadNextSectionAsync is called.
            Stream originalStream = section.Body;
            Stream seekableStream = null;
            bool disposeSeekableStream = false;

            try
            {
                seekableStream = _recyclableMemoryStreamManager.GetStream();

                try
                {
                    await originalStream.CopyToAsync(seekableStream, cancellationToken);
                }
                catch (IOException)
                {
                    // Unexpected end of the stream; this happens when the request is multi-part but has no sections.
                    disposeSeekableStream = true;

                    // We can return null here because it seems like after it encounters the IOException,
                    // next ReadNextSectionAsync will also throws IOException.
                    return null;
                }

                seekableStream.Seek(0, SeekOrigin.Begin);

                return new MultipartBodyPart(section.ContentType, seekableStream);
            }
            catch (Exception)
            {
                disposeSeekableStream = true;

                throw;
            }
            finally
            {
                if (disposeSeekableStream && seekableStream != null)
                {
                    await seekableStream.DisposeAsync();
                }

                await originalStream.DisposeAsync();
            }
        }
    }
}
