// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Delete;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Delete
{
    public class DeleteDicomResourcesHandler : IRequestHandler<DeleteDicomResourcesRequest, DeleteDicomResourcesResponse>
    {
        private readonly ILogger<DeleteDicomResourcesHandler> _logger;
        private readonly IDicomRouteProvider _dicomRouteProvider;
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public DeleteDicomResourcesHandler(
            ILogger<DeleteDicomResourcesHandler> logger,
            IDicomRouteProvider dicomRouteProvider,
            IDicomBlobDataStore dicomBlobDataStore,
            IDicomMetadataStore dicomMetadataStore)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));
            EnsureArg.IsNotNull(dicomBlobDataStore, nameof(dicomBlobDataStore));
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));

            _logger = logger;
            _dicomRouteProvider = dicomRouteProvider;
            _dicomBlobDataStore = dicomBlobDataStore;
            _dicomMetadataStore = dicomMetadataStore;
        }

        /// <inheritdoc />
        public async Task<DeleteDicomResourcesResponse> Handle(DeleteDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            if (string.IsNullOrEmpty(message.StudyInstanceUID))
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.BadRequest);
            }

            switch (message.ResourceType)
            {
                case ResourceType.Study:
                    return await HandleDeleteStudy(message, cancellationToken);
                case ResourceType.Series:
                    return await HandleDeleteSeries(message, cancellationToken);
                case ResourceType.Instance:
                    return await HandleDeleteInstance(message, cancellationToken);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.BadRequest);
        }

        private async Task<DeleteDicomResourcesResponse> HandleDeleteStudy(DeleteDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            // Delete metadata and retrieve list of instances
            IEnumerable<DicomInstance> instances;
            try
            {
                instances = await _dicomMetadataStore.DeleteStudyAsync(message.StudyInstanceUID, cancellationToken);
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when deleting study metadata.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            // Delete the blob for each instance
            try
            {
                await Task.WhenAll(instances.Select(x => _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(x), cancellationToken)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when deleting a study.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.OK);
        }

        private async Task<DeleteDicomResourcesResponse> HandleDeleteSeries(DeleteDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            // Delete metadata and retrieve list of instances
            IEnumerable<DicomInstance> instances;
            try
            {
                instances = await _dicomMetadataStore.DeleteSeriesAsync(message.StudyInstanceUID, message.SeriesUID, cancellationToken);
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when deleting series metadata.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            // Delete the blob for each instance
            try
            {
                await Task.WhenAll(instances.Select(x => _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(x), cancellationToken)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when deleting a series.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.OK);
        }

        private async Task<DeleteDicomResourcesResponse> HandleDeleteInstance(DeleteDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            // Delete metadata if the instance exists
            try
            {
                await _dicomMetadataStore.DeleteInstanceAsync(message.StudyInstanceUID, message.SeriesUID, message.InstanceUID, cancellationToken);
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when deleting instance metadata.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            // Delete the instance blob
            try
            {
                await _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(new DicomInstance(message.StudyInstanceUID, message.SeriesUID, message.InstanceUID)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when deleting the instance.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.OK);
        }

        private static string GetBlobStorageName(DicomInstance dicomInstance)
            => $"{dicomInstance.StudyInstanceUID}/{dicomInstance.SeriesInstanceUID}/{dicomInstance.SopInstanceUID}";
    }
}
