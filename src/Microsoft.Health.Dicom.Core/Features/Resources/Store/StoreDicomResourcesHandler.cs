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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Persistence.Store;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    public class StoreDicomResourcesHandler : IRequestHandler<StoreDicomResourcesRequest, StoreDicomResourcesResponse>
    {
        private const string ApplicationDicom = "application/dicom";
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;
        private readonly ILogger<StoreDicomResourcesHandler> _logger;
        private readonly IDicomRouteProvider _dicomRouteProvider;
        private readonly DicomDataStore _dicomDataStore;

        public StoreDicomResourcesHandler(
            ILogger<StoreDicomResourcesHandler> logger,
            IDicomRouteProvider dicomRouteProvider,
            DicomDataStore dicomDataStore)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));

            _logger = logger;
            _dicomRouteProvider = dicomRouteProvider;
            _dicomDataStore = dicomDataStore;
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

            bool unsupportedContentInRequest = false;
            MultipartReader reader = message.GetMultipartReader();
            MultipartSection section = await reader.ReadNextSectionAsync(cancellationToken);

            using (StoreTransaction storeTransaction = _dicomDataStore.BeginStoreTransaction())
            {
                while (section?.Body != null)
                {
                    if (section.ContentType != null)
                    {
                        switch (section.ContentType)
                        {
                            case ApplicationDicom:
                                await StoreApplicationDicomContentAsync(
                                                message.StudyInstanceUID, section.Body, storeTransaction, responseBuilder, cancellationToken);
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

                await storeTransaction.CommitAsync();
            }

            return responseBuilder.GetStoreResponse(unsupportedContentInRequest);
        }

        private async Task StoreApplicationDicomContentAsync(
            string studyInstanceUID,
            Stream stream,
            StoreTransaction storeTransaction,
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
                        var dicomFileStudyInstanceUID = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                        if (string.IsNullOrWhiteSpace(studyInstanceUID) ||
                            studyInstanceUID.Equals(dicomFileStudyInstanceUID, EqualsStringComparison))
                        {
                            await storeTransaction.StoreDicomFileAsync(seekStream, dicomFile, cancellationToken);
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
    }
}
