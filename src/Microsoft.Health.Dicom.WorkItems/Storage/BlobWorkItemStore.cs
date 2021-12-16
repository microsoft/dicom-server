// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Workitems;
using Microsoft.IO;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM instance work-item.
    /// </summary>
    public class BlobWorkitemStore : IWorkitemStore
    {
        private readonly BlobContainerClient _container;
        private readonly JsonSerializer _jsonSerializer;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public BlobWorkitemStore(
            BlobServiceClient client,
            JsonSerializer jsonSerializer,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(jsonSerializer, nameof(jsonSerializer));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            var containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _jsonSerializer = jsonSerializer;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        /// <inheritdoc />
        public async Task StoreInstanceWorkitemAsync(
            Workitem workItem,
            long version,
            CancellationToken cancellationToken)
        {
            // EnsureArg.IsNotNull(workItem, nameof(workItem));

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task DeleteInstanceWorkitemIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            var blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            await ExecuteAsync(() => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
        }

        /// <inheritdoc />
        public async Task<Workitem> GetInstanceWorkitemAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            var cloudBlockBlob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            return await Task.FromResult(default(Workitem));
        }

        private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            var blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}_metadata.json";

            return _container.GetBlockBlobClient(blobName);
        }

        private static async Task ExecuteAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
            {
                throw new ItemNotFoundException(ex);
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }
    }
}
