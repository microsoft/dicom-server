// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    public class StoreDicomResourcesHandler : IRequestHandler<StoreDicomResourcesRequest, StoreDicomResourcesResponse>
    {
        private const string ApplicationDicom = "application/dicom";
        private readonly IDicomDataStore _dicomDataStore;
        private readonly IDicomRouteProvider _dicomRouteProvider;

        public StoreDicomResourcesHandler(
            IDicomDataStore dicomDataStore,
            IDicomRouteProvider dicomRouteProvider)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));

            _dicomDataStore = dicomDataStore;
            _dicomRouteProvider = dicomRouteProvider;
        }

        public async Task<StoreDicomResourcesResponse> Handle(StoreDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            if (!message.IsMultipartRequest)
            {
                return new StoreDicomResourcesResponse(HttpStatusCode.UnsupportedMediaType);
            }

            var responseBuilder = new StoreTransactionResponseBuilder(message.RequestBaseUri, _dicomRouteProvider, message.StudyInstanceUID);

            MultipartReader reader = message.GetMultipartReader();
            MultipartSection section = await reader.ReadNextSectionAsync(cancellationToken);

            while (section?.Body != null)
            {
                if (section.ContentType == null || !section.ContentType.Equals(ApplicationDicom, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new StoreDicomResourcesResponse(HttpStatusCode.UnsupportedMediaType);
                }

                using (Stream stream = GetReadableStream(section.Body))
                {
                    section.Body.Dispose();

                    StoreOutcome storeOutcome = await _dicomDataStore.StoreDicomFileAsync(stream, message.StudyInstanceUID, cancellationToken);
                    responseBuilder.AddStoreOutcome(storeOutcome);
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
    }
}
