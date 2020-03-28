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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Persistence.Store;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomStoreService : IDicomStoreService
    {
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomInstanceMetadataStore;
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomRouteProvider _dicomRouteProvider;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger<StoreDicomResourcesHandler> _logger;
        private const string ApplicationDicom = "application/dicom";
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        public DicomStoreService(
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomInstanceMetadataStore,
            IDicomIndexDataStore dicomIndexDataStore,
            IDicomRouteProvider dicomRouteProvider,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            ILogger<StoreDicomResourcesHandler> logger)
        {
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomInstanceMetadataStore, nameof(dicomInstanceMetadataStore));
            EnsureArg.IsNotNull(dicomIndexDataStore, nameof(dicomIndexDataStore));
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomInstanceMetadataStore = dicomInstanceMetadataStore;
            _dicomIndexDataStore = dicomIndexDataStore;
            _dicomRouteProvider = dicomRouteProvider;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;
        }

        public async Task<StoreDicomResourcesResponse> StoreMultiPartDicomResourceAsync(
            Uri requestBaseUri,
            Stream contentStream,
            string requestContentType,
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(requestBaseUri, nameof(requestBaseUri));
            EnsureArg.IsNotNull(contentStream, nameof(contentStream));
            EnsureArg.IsNotNullOrWhiteSpace(requestContentType);

            var responseBuilder = new StoreResponseBuilder(requestBaseUri, _dicomRouteProvider, studyInstanceUid);
            _ = MediaTypeHeaderValue.TryParse(requestContentType, out MediaTypeHeaderValue media);
            string boundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();
            var multipartReader = new MultipartReader(boundary, contentStream);

            bool unsupportedContentInRequest = false;
            MultipartSection section = await multipartReader.ReadNextSectionAsync(cancellationToken);

            while (section?.Body != null)
            {
                if (section.ContentType != null)
                {
                    switch (section.ContentType)
                    {
                        case ApplicationDicom:
                            await StoreApplicationDicomContentAsync(
                                            studyInstanceUid, section.Body, responseBuilder, cancellationToken);
                            break;
                        default:
                            unsupportedContentInRequest = true;
                            break;
                    }
                }

                try
                {
                    section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                }
                catch (IOException)
                {
                    // Unexpected end of the stream; this happens when the request is multi-part but has no sections.
                    section = null;
                }
            }

            return responseBuilder.GetStoreResponse(unsupportedContentInRequest);
        }

        private async Task StoreApplicationDicomContentAsync(
            string studyInstanceUid,
            Stream stream,
            StoreResponseBuilder transactionResponseBuilder,
            CancellationToken cancellationToken)
        {
            DicomFile dicomFile = null;

            try
            {
                await using (Stream seekStream = _recyclableMemoryStreamManager.GetStream())
                {
                    // Copy stream to a memory stream so it can be seeked by the fo-dicom library.
                    await stream.CopyToAsync(seekStream, cancellationToken);
                    stream.Dispose();

                    seekStream.Seek(0, SeekOrigin.Begin);
                    dicomFile = await DicomFile.OpenAsync(seekStream);

                    if (dicomFile != null)
                    {
                        // Now Validate if the StudyInstanceUID is provided, it matches the provided file
                        var dicomFileStudyInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                        if (string.IsNullOrWhiteSpace(studyInstanceUid) ||
                            studyInstanceUid.Equals(dicomFileStudyInstanceUID, EqualsStringComparison))
                        {
                            await OrchestrateDicomFilePersistenceAsync(seekStream, dicomFile, cancellationToken);
                            transactionResponseBuilder.AddSuccess(dicomFile.Dataset);
                        }
                        else
                        {
                            transactionResponseBuilder.AddFailure(dicomFile.Dataset, StoreFailureCodes.MismatchStudyInstanceUIDFailureCode);
                        }

                        return;
                    }
                }
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.Conflict)
            {
                transactionResponseBuilder.AddFailure(dicomFile.Dataset, StoreFailureCodes.SopInstanceAlredyExistsFailureCode);
                return;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when storing an instance.");
            }

            transactionResponseBuilder.AddFailure(dicomFile?.Dataset);
        }

        private async Task OrchestrateDicomFilePersistenceAsync(Stream dicomFileStream, DicomFile dicomFile, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFileStream, nameof(dicomFileStream));
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));

            var identifier = DicomDatasetIdentifier.Create(dicomFile.Dataset);

            // TODO fix the verion once we implement the data consistency
            var dicomInstanceIdentifier = new DicomInstanceIdentifier(
                identifier.StudyInstanceUid,
                identifier.SeriesInstanceUid,
                identifier.SopInstanceUid,
                version: 0);

            // If a file with the same name exists, a conflict exception will be thrown.
            dicomFileStream.Seek(0, SeekOrigin.Begin);
            await _dicomBlobDataStore.AddFileAsStreamAsync(dicomInstanceIdentifier, dicomFileStream, cancellationToken: cancellationToken);

            // Strip the DICOM file down to the tags we want to store for metadata.
            dicomFile.Dataset.RemoveBulkDataVrs();
            await _dicomInstanceMetadataStore.AddInstanceMetadataAsync(dicomFile.Dataset);
            await _dicomIndexDataStore.IndexInstanceAsync(dicomFile.Dataset);
        }
    }
}
