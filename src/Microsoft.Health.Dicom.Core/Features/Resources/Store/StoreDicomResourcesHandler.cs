// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    public class StoreDicomResourcesHandler : IRequestHandler<StoreDicomResourcesRequest, StoreDicomResourcesResponse>
    {
        private const string ApplicationDicom = "application/dicom";
        private readonly IDicomRouteProvider _dicomRouteProvider;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public StoreDicomResourcesHandler(
            IDicomRouteProvider dicomRouteProvider,
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));

            _dicomRouteProvider = dicomRouteProvider;
            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomMetadataStore = dicomMetadataStore;
        }

        /// <inheritdoc />
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
            IList<DicomDataset> metadataInstances = new List<DicomDataset>();

            bool unsupportedContentInRequest = false;

            while (section?.Body != null)
            {
                if (section.ContentType != null)
                {
                    DicomDataset metadataInstance = null;

                    switch (section.ContentType)
                    {
                        case ApplicationDicom:
                            metadataInstance = await StoreApplicationDicomContentAsync(
                                                                    message.StudyInstanceUID, section.Body, responseBuilder, cancellationToken);
                            break;
                        default:
                            unsupportedContentInRequest = true;
                            break;
                    }

                    if (metadataInstance != null)
                    {
                        metadataInstances.Add(metadataInstance);
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

            // Now store all the metadata and return the result.
            if (metadataInstances.Count > 0)
            {
                await _dicomMetadataStore.AddStudySeriesDicomMetadataAsync(metadataInstances, cancellationToken);
            }

            return responseBuilder.GetStoreResponse(unsupportedContentInRequest);
        }

        private async Task<DicomDataset> StoreApplicationDicomContentAsync(
            string studyInstanceUID,
            Stream stream,
            StoreTransactionResponseBuilder transactionResponseBuilder,
            CancellationToken cancellationToken)
        {
            DicomFile dicomFile = null;

            try
            {
                using (Stream seekStream = new MemoryStream())
                {
                    // Copy stream to a memory stream so it can be seeked by the fo-dicom library.
                    stream.CopyTo(seekStream);
                    stream.Dispose();

                    seekStream.Seek(0, SeekOrigin.Begin);

                    dicomFile = await DicomFile.OpenAsync(seekStream);

                    if (dicomFile != null)
                    {
                        // Now Validate if the StudyInstanceUID is provided, it matches the provided file
                        var dicomInstance = DicomInstance.Create(dicomFile.Dataset);
                        if (string.IsNullOrWhiteSpace(studyInstanceUID) ||
                            studyInstanceUID.Equals(dicomInstance.StudyInstanceUID, DicomStudy.EqualsStringComparison))
                        {
                            seekStream.Seek(0, SeekOrigin.Begin);
                            await _dicomBlobDataStore.AddFileAsStreamAsync(GetBlobStorageName(dicomInstance), seekStream, cancellationToken: cancellationToken);
                            transactionResponseBuilder.AddSuccess(dicomFile.Dataset);
                            return dicomFile.Dataset;
                        }
                        else
                        {
                            transactionResponseBuilder.AddFailure(dicomFile.Dataset, StoreTransactionResponseBuilder.MismatchStudyInstanceUIDFailureCode);
                            return null;
                        }
                    }
                }
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.Conflict)
            {
                transactionResponseBuilder.AddFailure(dicomFile.Dataset, StoreTransactionResponseBuilder.SopInstanceAlredyExistsFailureCode);
                return null;
            }
            catch
            {
            }

            transactionResponseBuilder.AddFailure(dicomFile?.Dataset);
            return null;
        }

        internal static string GetBlobStorageName(DicomInstance dicomInstance)
            => $"{dicomInstance.StudyInstanceUID}/{dicomInstance.SeriesInstanceUID}/{dicomInstance.SopInstanceUID}";
    }
}
