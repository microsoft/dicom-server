// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    public class StoreDicomResourcesHandler : IRequestHandler<StoreDicomResourcesRequest, StoreDicomResourcesResponse>
    {
        private const string ApplicationDicom = "application/dicom";
        private readonly DicomDataStore _dicomDataStore;

        public StoreDicomResourcesHandler(
            IDicomBlobDataStore blobDataStore,
            IDicomMetadataStore metadataStore,
            IDicomIndexDataStore indexDataStore)
        {
            _dicomDataStore = new DicomDataStore(blobDataStore, metadataStore, indexDataStore);
        }

        /// <inheritdoc />
        public async Task<StoreDicomResourcesResponse> Handle(StoreDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            if (!message.IsMultipartRequest)
            {
                return new StoreDicomResourcesResponse(HttpStatusCode.UnsupportedMediaType);
            }

            MultipartReader reader = message.GetMultipartReader();
            MultipartSection section = await reader.ReadNextSectionAsync(cancellationToken);

            while (section?.Body != null)
            {
                if (section.ContentType != null)
                {
                    switch (section.ContentType)
                    {
                        case ApplicationDicom:
                            await StoreApplicationDicomContentAsync(section.Body, message.StudyInstanceUID, cancellationToken);
                            break;
                        default:
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

            return new StoreDicomResourcesResponse(HttpStatusCode.OK);
        }

        private async Task<DicomInstance> StoreApplicationDicomContentAsync(Stream stream, string studyInstanceUID, CancellationToken cancellationToken)
        {
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
                        var dicomInstance = DicomInstance.Create(dicomFile.Dataset);

                        if (!string.Equals(dicomInstance.StudyInstanceUID, studyInstanceUID, StringComparison.InvariantCultureIgnoreCase))
                        {
                            throw new Exception("The provided study instance UID does not match the files study instance UID");
                        }

                        await _dicomDataStore.StoreAsync(seekStream, dicomFile, cancellationToken);
                        return dicomInstance;
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
