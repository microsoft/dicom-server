// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM workitem instance.
    /// </summary>
    public class BlobWorkitemStore : IWorkitemStore
    {
        private readonly BlobContainerClient _container;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public BlobWorkitemStore(
            BlobServiceClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            IOptions<JsonSerializerOptions> jsonSerializerOptions)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(jsonSerializerOptions?.Value, nameof(jsonSerializerOptions));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            var containerConfiguration = namedBlobContainerConfigurationAccessor
                .Get(Constants.WorkitemContainerConfigurationName);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _jsonSerializerOptions = jsonSerializerOptions.Value;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(
            WorkitemInstanceIdentifier identifier,
            DicomDataset dataset,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            var addWorkitemStreamTagName = nameof(BlobWorkitemStore) + "." + nameof(AddWorkitemAsync);
            var blob = GetBlockBlobClient(identifier);

            try
            {
                await using (Stream stream = _recyclableMemoryStreamManager.GetStream(addWorkitemStreamTagName))
                using (Utf8JsonWriter utf8Writer = new Utf8JsonWriter(stream))
                {
                    JsonSerializer.Serialize(utf8Writer, dataset, _jsonSerializerOptions);
                    await utf8Writer.FlushAsync(cancellationToken);
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
        public async Task<DicomDataset> GetWorkitemAsync(WorkitemInstanceIdentifier workitemInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(workitemInstanceIdentifier, nameof(workitemInstanceIdentifier));
            var getWorkitemStreamTagName = nameof(BlobWorkitemStore) + "." + nameof(GetWorkitemAsync);

            BlockBlobClient cloudBlockBlob = GetBlockBlobClient(workitemInstanceIdentifier);

            try
            {
                await using (Stream stream = _recyclableMemoryStreamManager.GetStream(getWorkitemStreamTagName))
                {
                    await cloudBlockBlob.DownloadToAsync(stream, cancellationToken);

                    stream.Seek(0, SeekOrigin.Begin);

                    return await JsonSerializer.DeserializeAsync<DicomDataset>(stream, _jsonSerializerOptions, cancellationToken);
                }
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

        private BlockBlobClient GetBlockBlobClient(WorkitemInstanceIdentifier identifier)
        {
            var blobName = $"{identifier.WorkitemUid}_{identifier.WorkitemKey}_{identifier.Watermark}_workitem.json";

            return _container.GetBlockBlobClient(blobName);
        }
    }
}
