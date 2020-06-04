// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM instance metadata.
    /// </summary>
    public class BlobMetadataStore : IMetadataStore
    {
        private static readonly string GetInstanceMetadataStreamTagName = $"{nameof(BlobMetadataStore)}.{nameof(GetInstanceMetadataAsync)}";
        private static readonly string StoreInstanceMetadataStreamTagName = $"{nameof(BlobMetadataStore)}.{nameof(StoreInstanceMetadataAsync)}";
        private static readonly Encoding _metadataEncoding = Encoding.UTF8;

        private readonly BlobContainerClient _container;
        private readonly JsonSerializer _jsonSerializer;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public BlobMetadataStore(
            BlobServiceClient client,
            JsonSerializer jsonSerializer,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(jsonSerializer, nameof(jsonSerializer));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _jsonSerializer = jsonSerializer;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        /// <inheritdoc />
        public async Task StoreInstanceMetadataAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Creates a copy of the dataset with bulk data removed.
            DicomDataset dicomDatasetWithoutBulkData = dicomDataset.CopyWithoutBulkDataItems();

            BlockBlobClient blob = GetInstanceBlockBlob(dicomDatasetWithoutBulkData.ToVersionedInstanceIdentifier(version));

            try
            {
                await using (Stream stream = _recyclableMemoryStreamManager.GetStream(StoreInstanceMetadataStreamTagName))
                await using (var streamWriter = new StreamWriter(stream, _metadataEncoding))
                using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                {
                    _jsonSerializer.Serialize(jsonTextWriter, dicomDatasetWithoutBulkData);
                    jsonTextWriter.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadAsync(
                        stream,
                        new BlobHttpHeaders()
                        {
                            ContentType = KnownContentTypes.ApplicationJson,
                        },
                        metadata: null,
                        conditions: null,
                        accessTier: null,
                        progressHandler: null,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new DataStoreException(ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteInstanceMetadataIfExistsAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            BlockBlobClient blob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            await ExecuteAsync(() => blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions: null, cancellationToken));
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetInstanceMetadataAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken)
        {
            BlockBlobClient cloudBlockBlob = GetInstanceBlockBlob(versionedInstanceIdentifier);

            DicomDataset dicomDataset = null;

            await ExecuteAsync(async () =>
            {
                await using (Stream stream = _recyclableMemoryStreamManager.GetStream(GetInstanceMetadataStreamTagName))
                {
                    await cloudBlockBlob.DownloadToAsync(stream, cancellationToken);

                    stream.Seek(0, SeekOrigin.Begin);

                    using (var streamReader = new StreamReader(stream, _metadataEncoding))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        dicomDataset = _jsonSerializer.Deserialize<DicomDataset>(jsonTextReader);
                    }
                }
            });

            return dicomDataset;
        }

        private BlockBlobClient GetInstanceBlockBlob(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            var blobName = $"{versionedInstanceIdentifier.StudyInstanceUid}/{versionedInstanceIdentifier.SeriesInstanceUid}/{versionedInstanceIdentifier.SopInstanceUid}_{versionedInstanceIdentifier.Version}_metadata.json";

            return _container.GetBlockBlobClient(blobName);
        }

        private async Task ExecuteAsync(Func<Task> action)
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
