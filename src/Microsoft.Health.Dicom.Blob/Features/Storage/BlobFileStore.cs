// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM files using the Azure Blob storage.
    /// </summary>
    public class BlobFileStore : IFileStore
    {
        private const string GetFileStreamTagName = nameof(BlobFileStore) + "." + nameof(GetFileAsync);
        private readonly BlobContainerClient _container;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration;

        public BlobFileStore(
            BlobServiceClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            BlobDataStoreConfiguration blobDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            EnsureArg.IsNotNull(blobDataStoreConfiguration, nameof(blobDataStoreConfiguration));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _blobDataStoreConfiguration = blobDataStoreConfiguration;
        }

        /// <inheritdoc />
        public async Task<Uri> StoreFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            Stream stream,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
            EnsureArg.IsNotNull(stream, nameof(stream));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);
            stream.Seek(0, SeekOrigin.Begin);

            try
            {
                await blob.UploadAsync(
                    stream,
                    new BlobHttpHeaders()
                    {
                        ContentType = KnownContentTypes.ApplicationDicom,
                    },
                    metadata: null,
                    conditions: null,
                    accessTier: null,
                    progressHandler: null,
                    cancellationToken);

                return blob.Uri;
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteFileIfExistsAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            await ExecuteAsync(() => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
        }

        /// <inheritdoc />
        public async Task<Stream> GetFileAsync(
            VersionedInstanceIdentifier versionedInstanceIdentifier,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));

            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            Stream stream = null;
            StorageTransferOptions storageTransferOptions = new StorageTransferOptions
            {
                MaximumConcurrency = _blobDataStoreConfiguration.RequestOptions.DownloadMaximumConcurrency,
            };

            await ExecuteAsync(async () =>
            {
                stream = _recyclableMemoryStreamManager.GetStream(GetFileStreamTagName);
                await blob.DownloadToAsync(stream, conditions: null, storageTransferOptions, cancellationToken);
            });
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            string blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}.dcm";

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
