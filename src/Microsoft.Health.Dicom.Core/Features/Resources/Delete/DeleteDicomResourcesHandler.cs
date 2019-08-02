// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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

            if ((!message.IsBodyEmpty) || string.IsNullOrEmpty(message.StudyInstanceUID))
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(message.SeriesUID) && string.IsNullOrEmpty(message.InstanceUID))
            {
                return await HandleDeleteStudy(message, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(message.SeriesUID) && string.IsNullOrEmpty(message.InstanceUID))
            {
                return await HandleDeleteSeries(message, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(message.SeriesUID) && !string.IsNullOrEmpty(message.InstanceUID))
            {
                return await HandleDeleteInstance(message, cancellationToken).ConfigureAwait(false);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.BadRequest);
        }

        private async Task<DeleteDicomResourcesResponse> HandleDeleteStudy(DeleteDicomResourcesRequest message, CancellationToken cancellationToken)
        {
            // Get Instances in the study
            IEnumerable<DicomInstance> instances;
            try
            {
                instances = await _dicomMetadataStore.GetInstancesInStudyAsync(message.StudyInstanceUID, cancellationToken).ConfigureAwait(false);
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when retreiving instances from study.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            // Delete from Blob & Metadata
            try
            {
                foreach (DicomInstance instance in instances)
                {
                    await _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(instance), cancellationToken).ConfigureAwait(false);
                }

                await _dicomMetadataStore.DeleteStudyAsync(message.StudyInstanceUID, cancellationToken).ConfigureAwait(false);
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
            // Get Instances in the series
            IEnumerable<DicomInstance> instances;
            try
            {
                instances = await _dicomMetadataStore.GetInstancesInSeriesAsync(message.StudyInstanceUID, message.SeriesUID, cancellationToken).ConfigureAwait(false);
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when retreiving instances from series.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            // Delete the instances
            try
            {
                foreach (DicomInstance instance in instances)
                {
                    await _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(instance), cancellationToken).ConfigureAwait(false);
                }

                await _dicomMetadataStore.DeleteSeriesAsync(message.StudyInstanceUID, message.SeriesUID, cancellationToken).ConfigureAwait(false);
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
            // Check instance is part of the series (and series exists)
            DicomInstance instanceToDelete = null;
            try
            {
                IEnumerable<DicomInstance> instances = await _dicomMetadataStore.GetInstancesInSeriesAsync(message.StudyInstanceUID, message.SeriesUID, cancellationToken).ConfigureAwait(false);
                foreach (DicomInstance instance in instances)
                {
                    if (instance.SopInstanceUID == message.InstanceUID)
                    {
                        instanceToDelete = instance;
                    }
                }

                if (instanceToDelete == null)
                {
                    return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
                }
            }
            catch (DataStoreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                return new DeleteDicomResourcesResponse(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception when retreiving instances from series.");
                return new DeleteDicomResourcesResponse(HttpStatusCode.InternalServerError);
            }

            // Delete the instance
            try
            {
                await _dicomBlobDataStore.DeleteFileIfExistsAsync(GetBlobStorageName(instanceToDelete)).ConfigureAwait(false);
                await _dicomMetadataStore.DeleteInstanceAsync(message.StudyInstanceUID, message.SeriesUID, message.InstanceUID, cancellationToken).ConfigureAwait(false);
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
