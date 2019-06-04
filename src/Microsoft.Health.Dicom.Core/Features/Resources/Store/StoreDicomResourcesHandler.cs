// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    public class StoreDicomResourcesHandler : IRequestHandler<StoreDicomResourcesRequest, StoreDicomResourcesResponse>
    {
        private const string ApplicationDicom = "application/dicom";
        private const bool OverwriteFiles = false;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomRouteProvider _dicomRouteProvider;

        public StoreDicomResourcesHandler(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomRouteProvider dicomRouteProvider)
        {
            _dicomBlobDataStore = EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            _dicomRouteProvider = EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));
        }

        public async Task<StoreDicomResourcesResponse> Handle(StoreDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            if (!message.IsMultipartRequest)
            {
                return new StoreDicomResourcesResponse(HttpStatusCode.UnsupportedMediaType);
            }

            var responseBuilder = new WadoStoreResponseBuilder(message.BaseAddress, _dicomRouteProvider, message.StudyInstanceUID);

            MultipartReader reader = message.GetMultipartReader();
            MultipartSection section = await reader.ReadNextSectionAsync(cancellationToken);

            while (section?.Body != null)
            {
                // TODO: We should delete any stored items if this is true at any point.
                if (section.ContentType == null || !section.ContentType.Equals(ApplicationDicom, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new StoreDicomResourcesResponse(HttpStatusCode.UnsupportedMediaType);
                }

                using (Stream stream = GetReadableStream(section.Body))
                {
                    await ProcessPartAsync(message.BaseAddress, responseBuilder, stream, cancellationToken);
                }

                section = await reader.ReadNextSectionAsync(cancellationToken);
            }

            return new StoreDicomResourcesResponse(responseBuilder.StatusCode, responseBuilder.GetResponseDataset());
        }

        /// <summary>
        /// Method to convert the input stream to a readable stream by fo-dicom.
        /// DicomFile.OpenAsync() requires a MemoryStream.
        /// </summary>
        /// <param name="inputStream">The input stream that will get copied to a memory stream</param>
        /// <returns>The strean.</returns>
        private static Stream GetReadableStream(Stream inputStream)
        {
            var memoryStream = new MemoryStream();
            inputStream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        private static string GetUniqueBlobName(DicomIdentity dicomIdentity)
            => $"{dicomIdentity.StudyInstanceUID}\\{dicomIdentity.SeriesInstanceUID}\\{Guid.NewGuid().ToString("n", CultureInfo.InvariantCulture)}";

        private async Task ProcessPartAsync(string host, WadoStoreResponseBuilder responseBuilder, Stream stream, CancellationToken cancellationToken)
        {
            DicomIdentity dicomIdentity = null;

            try
            {
                DicomFile dicomFile = await DicomFile.OpenAsync(stream);
                dicomIdentity = new DicomIdentity(dicomFile.Dataset);

                // Check the DICOM identity is valid
                if (dicomIdentity.IsValid)
                {
                    string blobName = GetUniqueBlobName(dicomIdentity);

                    // Re-seek and save the original content.
                    stream.Seek(0, SeekOrigin.Begin);
                    await _dicomBlobDataStore.AddFileAsStreamAsync(blobName, stream, overwriteIfExists: OverwriteFiles, cancellationToken);

                    responseBuilder.AddResult(host, dicomIdentity);
                }
                else
                {
                    responseBuilder.AddFailure(dicomIdentity);
                }
            }
            catch
            {
                responseBuilder.AddFailure(dicomIdentity);
            }
        }
    }
}
