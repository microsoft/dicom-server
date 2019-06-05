// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
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

            bool unsupportedContentInRequest = false;

            while (section?.Body != null)
            {
                if (section.ContentType != null)
                {
                    switch (section.ContentType)
                    {
                        case ApplicationDicom:
                            StoreOutcome storeOutcome = await StoreApplicationDicomContentAsync(section.Body, message.StudyInstanceUID, cancellationToken);
                            responseBuilder.AddStoreOutcome(storeOutcome);
                            break;
                        default:
                            unsupportedContentInRequest = true;
                            break;
                    }
                }

                try
                {
                    section = await reader.ReadNextSectionAsync(cancellationToken);
                }
                catch (IOException)
                {
                    // Unexpected end of the stream; this happens when the request is multi-part but has no sections.
                    section = null;
                }
            }

            return responseBuilder.GetStoreResponse(unsupportedContentInRequest);
        }

        private async Task<StoreOutcome> StoreApplicationDicomContentAsync(Stream stream, string studyInstanceUID, CancellationToken cancellationToken)
        {
            DicomIdentity dicomIdentity = null;

            try
            {
                using (Stream seekStream = new MemoryStream())
                {
                    // Copy stream to a memory stream so it can be seeked by the fo-dicom library.
                    stream.CopyTo(seekStream);
                    stream.Dispose();

                    seekStream.Seek(0, SeekOrigin.Begin);

                    DicomFile dicomFile = await DicomFile.OpenAsync(seekStream);

                    if (dicomFile != null)
                    {
                        dicomIdentity = new DicomIdentity(dicomFile.Dataset);

                        bool isStored = await _dicomDataStore.StoreDicomFileAsync(dicomFile, studyInstanceUID, cancellationToken);
                        return new StoreOutcome(dicomIdentity, isStored);
                    }
                }
            }
            catch
            {
            }

            return new StoreOutcome(dicomIdentity, false);
        }
    }
}
