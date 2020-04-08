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
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Api.Web
{
    /// <summary>
    /// Multipart reader implemented by using AspNetCore's <see cref="MultipartReader"/>.
    /// </summary>
    internal class AspNetCoreMultipartReader : IMultipartReader
    {
        private readonly ISeekableStreamConverter _seekableStreamConverter;
        private readonly MultipartReader _multipartReader;

        internal AspNetCoreMultipartReader(
            string contentType,
            Stream body,
            ISeekableStreamConverter seekableStreamConverter)
        {
            EnsureArg.IsNotNull(contentType, nameof(contentType));
            EnsureArg.IsNotNull(body, nameof(body));
            EnsureArg.IsNotNull(seekableStreamConverter, nameof(seekableStreamConverter));

            _seekableStreamConverter = seekableStreamConverter;

            if (!MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue media) ||
                !media.MediaType.Equals(KnownContentTypes.MultipartRelated, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomApiResource.UnsupportedContentType, contentType));
            }

            string boundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();

            if (string.IsNullOrWhiteSpace(boundary))
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

            try
            {
                // The stream must be consumed before the next ReadNextSectionAsync is called.
                // Also, the stream returned by the MultipartReader is not seekable. We need to make
                // it seekable so that we can process the stream multiple times.
                return new MultipartBodyPart(
                    section.ContentType,
                    await _seekableStreamConverter.ConvertAsync(section.Body, cancellationToken));
            }
            catch (MissingMultipartBodyPartException)
            {
                // We can terminate here because it seems like after it encounters the IOException,
                // next ReadNextSectionAsync will also throws IOException.
                return null;
            }
            finally
            {
                await section.Body.DisposeAsync();
            }
        }
    }
}
