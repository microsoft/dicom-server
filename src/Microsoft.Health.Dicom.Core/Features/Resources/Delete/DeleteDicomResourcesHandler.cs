// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
using Microsoft.Health.Dicom.Core.Features.Resources.Store;
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

            // Delete metadata and retrieve list of instances
            IEnumerable<DicomInstance> instancesToDelete = null;
            try
            {
                switch (message.ResourceType)
                {
                    case DeleteResourceType.Study:
                        instancesToDelete = await _dicomMetadataStore.DeleteStudyAsync(message.StudyInstanceUID, cancellationToken);
                        break;
                    case DeleteResourceType.Series:
                        instancesToDelete = await _dicomMetadataStore.DeleteSeriesAsync(message.StudyInstanceUID, message.SeriesUID, cancellationToken);
                        break;
                    case DeleteResourceType.Instance:
                        await _dicomMetadataStore.DeleteInstanceAsync(message.StudyInstanceUID, message.SeriesUID, message.InstanceUID, cancellationToken);
                        instancesToDelete = new DicomInstance[] { new DicomInstance(message.StudyInstanceUID, message.SeriesUID, message.InstanceUID) };
                        break;
                }
            }
            catch (DataStoreException e)
            {
                return new DeleteDicomResourcesResponse((HttpStatusCode)e.StatusCode);
            }

            // Delete instance blobs
            try
            {
                await Task.WhenAll(instancesToDelete.Select(x => _dicomBlobDataStore.DeleteFileIfExistsAsync(StoreDicomResourcesHandler.GetBlobStorageName(x), cancellationToken)));
            }
            catch (DataStoreException e)
            {
                return new DeleteDicomResourcesResponse((HttpStatusCode)e.StatusCode);
            }

            return new DeleteDicomResourcesResponse(HttpStatusCode.OK);
        }
    }
}
