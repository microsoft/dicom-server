// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Store.Upload
{
    /// <summary>
    /// Provides functionality to read uploaded DICOM instance from HTTP multipart request.
    /// </summary>
    public class UploadedDicomInstanceFromMultipartRequestReader : IUploadedDicomInstanceReader
    {
        private readonly IMultipartReaderFactory _multipartReaderFactory;
        private readonly ILogger _logger;

        public UploadedDicomInstanceFromMultipartRequestReader(
            IMultipartReaderFactory multipartReaderFactory,
            ILogger<UploadedDicomInstanceFromMultipartRequestReader> logger)
        {
            EnsureArg.IsNotNull(multipartReaderFactory, nameof(multipartReaderFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _multipartReaderFactory = multipartReaderFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool CanRead(string contentType)
        {
            return MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue media) &&
                string.Equals(KnownContentTypes.MultipartRelated, media.MediaType, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<IUploadedDicomInstance>> ReadAsync(string contentType, Stream body, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(body, nameof(body));

            IMultipartReader multipartReader = _multipartReaderFactory.Create(contentType, body);

            var uploadedDicomResources = new List<StreamOriginatedDicomInstance>();

            MultipartBodyPart bodyPart;

            try
            {
                while ((bodyPart = await multipartReader.ReadNextBodyPartAsync(cancellationToken)) != null)
                {
                    // Check the content type to make sure we can process.
                    if (!KnownContentTypes.ApplicationDicom.Equals(bodyPart.ContentType, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // TODO: Currently, we only support application/dicom. Support for metadata + bulkdata is coming.
                        throw new UnsupportedMediaTypeException(
                            string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, bodyPart.ContentType));
                    }

                    uploadedDicomResources.Add(new StreamOriginatedDicomInstance(bodyPart.Body));
                }
            }
            catch (Exception ex)
            {
                // Encountered an error while processing, release all resources.
                _logger.LogWarning(ex, "Failed to read the multipart message.");

                IEnumerable<Task> disposeTasks = uploadedDicomResources.Select(DisposeResourceAsync);

                await Task.WhenAll(disposeTasks);

                throw;
            }

            return uploadedDicomResources;
        }

        private async Task DisposeResourceAsync(IUploadedDicomInstance resource)
        {
            try
            {
                await resource.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose the resource.");
            }
        }
    }
}
