// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Provides functionality to read DICOM instance entries from HTTP application/dicom request.
    /// </summary>
    public class DicomInstanceEntryReaderForSinglePartRequest : IDicomInstanceEntryReader
    {
        private readonly ILogger _logger;
        private readonly ISeekableStreamConverter _seekableStreamConverter;

        public DicomInstanceEntryReaderForSinglePartRequest(
            ILogger<DicomInstanceEntryReaderForSinglePartRequest> logger,
            ISeekableStreamConverter seekableStreamConverter)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(seekableStreamConverter, nameof(seekableStreamConverter));

            _logger = logger;
            _seekableStreamConverter = seekableStreamConverter;
        }

        /// <inheritdoc />
        public bool CanRead(string contentType)
        {
            return MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue media) &&
                string.Equals(KnownContentTypes.ApplicationDicom, media.MediaType, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<IDicomInstanceEntry>> ReadAsync(string contentType, Stream body, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNullOrWhiteSpace(contentType, nameof(contentType));
            EnsureArg.IsNotNull(body, nameof(body));

            var dicomInstanceEntries = new List<StreamOriginatedDicomInstanceEntry>();

            if (!KnownContentTypes.ApplicationDicom.Equals(contentType, StringComparison.InvariantCultureIgnoreCase))
            {
                // TODO: Currently, we only support application/dicom. Support for metadata + bulkdata is coming.
                throw new UnsupportedMediaTypeException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, contentType));
            }

            Stream seekableStream = await _seekableStreamConverter.ConvertAsync(body);
            dicomInstanceEntries.Add(new StreamOriginatedDicomInstanceEntry(seekableStream));

            return dicomInstanceEntries;
        }
    }
}
