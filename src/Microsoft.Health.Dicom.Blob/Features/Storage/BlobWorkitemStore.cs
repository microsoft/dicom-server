// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.IO;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    /// <summary>
    /// Provides functionality for managing the DICOM instance metadata.
    /// </summary>
    public class BlobWorkitemStore : IWorkitemStore
    {
        private const string AddWorkitemStreamTagName = nameof(BlobWorkitemStore) + "." + nameof(AddWorkitemAsync);
        private static readonly Encoding DataEncoding = Encoding.UTF8;

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

            var containerConfiguration = namedBlobContainerConfigurationAccessor
                .Get(Constants.WorkitemContainerConfigurationName);

            _container = client.GetBlobContainerClient(containerConfiguration.ContainerName);
            _jsonSerializer = jsonSerializer;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        /// <inheritdoc />
        public async Task AddWorkitemAsync(
            WorkitemInstanceIdentifier identifier,
            DicomDataset dataset,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            var blob = GetBlockBlobClient(identifier);

            try
            {
                await using (Stream stream = _recyclableMemoryStreamManager.GetStream(AddWorkitemStreamTagName))
                await using (var streamWriter = new StreamWriter(stream, DataEncoding))
                using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                {
                    _jsonSerializer.Serialize(jsonTextWriter, dataset);
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

        private BlockBlobClient GetBlockBlobClient(WorkitemInstanceIdentifier identifier)
        {
            var blobName = $"{identifier.WorkitemUid}_{identifier.WorkitemKey}_workitem.json";

            return _container.GetBlockBlobClient(blobName);
        }
    }
}
