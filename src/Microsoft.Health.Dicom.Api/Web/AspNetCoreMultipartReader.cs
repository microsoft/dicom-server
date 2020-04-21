// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private const string TypeParameterName = "type";

        private readonly ISeekableStreamConverter _seekableStreamConverter;
        private readonly MultipartReader _multipartReader;

        private readonly string _rootContentType;
        private int _sectionIndex;

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

            // Check to see if the root content type was specified or not.
            NameValueHeaderValue rootContentTypeParameter = media.Parameters?.FirstOrDefault(
                parameter => TypeParameterName.Equals(parameter.Name.ToString(), StringComparison.InvariantCultureIgnoreCase));

            if (rootContentTypeParameter != null)
            {
                _rootContentType = HeaderUtilities.RemoveQuotes(rootContentTypeParameter.Value).ToString();
            }
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
                string contentType = section.ContentType;

                if (contentType == null && _sectionIndex == 0)
                {
                    // Based on RFC2387 Section 3.1, the content type of the "root" section
                    // can be specified through the request's Content-Type header. If the content
                    // type is not specified in the section and this is the "root" section,
                    // then check to see if it was specified in the request's Content-Type.
                    // TODO: For now, we are assuming the first section is the "root". However
                    // according to RFC2387 3.2, the root section can be specified by the
                    // start parameter. Add support later.
                    contentType = _rootContentType;
                }

                _sectionIndex++;

                // The stream must be consumed before the next ReadNextSectionAsync is called.
                // Also, the stream returned by the MultipartReader is not seekable. We need to make
                // it seekable so that we can process the stream multiple times.
                return new MultipartBodyPart(
                    contentType,
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
