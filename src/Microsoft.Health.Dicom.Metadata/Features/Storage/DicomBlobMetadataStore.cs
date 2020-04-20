// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.IO;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    internal class DicomBlobMetadataStore : IDicomMetadataStore
    {
        private const int MaximumRetryFailedRequests = 5;
        private static readonly Encoding _metadataEncoding = Encoding.UTF8;
        private readonly Random _random = new Random();
        private readonly CloudBlobContainer _container;
        private readonly JsonSerializer _jsonSerializer;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly ILogger<DicomBlobMetadataStore> _logger;

        public DicomBlobMetadataStore(
            CloudBlobClient client,
            JsonSerializer jsonSerializer,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            ILogger<DicomBlobMetadataStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(jsonSerializer, nameof(jsonSerializer));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _jsonSerializer = jsonSerializer;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _logger = logger;
        }

        public async Task AddInstanceMetadataAsync(DicomDataset dicomDataset, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var dicomInstance = dicomDataset.ToDicomInstanceIdentifier();
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(dicomInstance);

            IAsyncPolicy retryPolicy = CreateTooManyRequestsRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Storing Instance Metadata: {dicomInstance}");

                    await using (Stream stream = _recyclableMemoryStreamManager.GetStream())
                    await using (var streamWriter = new StreamWriter(stream, _metadataEncoding))
                    using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                    {
                        _jsonSerializer.Serialize(jsonTextWriter, dicomDataset);
                        jsonTextWriter.Flush();

                        stream.Seek(0, SeekOrigin.Begin);
                        await blockBlob.UploadFromStreamAsync(
                               stream,
                               AccessCondition.GenerateIfNotExistsCondition(),
                               new BlobRequestOptions(),
                               new OperationContext(),
                               cancellationToken);
                    }
                },
                retryPolicy);
        }

        public async Task DeleteInstanceMetadataIfExistsAsync(DicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken)
        {
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(dicomInstanceIdentifier);

            IAsyncPolicy retryPolicy = CreateTooManyRequestsRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Deleting Instance Metadata: {dicomInstanceIdentifier}");
                    await cloudBlockBlob.DeleteIfExistsAsync(cancellationToken);
                },
                retryPolicy);
        }

        public async Task<DicomDataset> GetInstanceMetadataAsync(DicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken)
        {
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(dicomInstanceIdentifier);

            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Getting Instance Metadata: {dicomInstanceIdentifier}");

                    await using (Stream stream = await cloudBlockBlob.OpenReadAsync(cancellationToken))
                    using (var streamReader = new StreamReader(stream, _metadataEncoding))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        return _jsonSerializer.Deserialize<DicomDataset>(jsonTextReader);
                    }
                });
        }

        private IAsyncPolicy CreateTooManyRequestsRetryPolicy()
           => Policy
                   .Handle<StorageException>(ex => ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.TooManyRequests ||
                                                    ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.BadRequest)
                   .WaitAndRetryAsync(MaximumRetryFailedRequests, retryIndex =>
                   {
                       // Otherwise, delay retry with some randomness.
                       return TimeSpan.FromMilliseconds((retryIndex - 1) * _random.Next(200, 500));
                   });

        // TODO remove this when AddInstance takes DicomInstanceIdentifier instead of just Dataset
        private CloudBlockBlob GetInstanceBlockBlob(DicomInstanceIdentifier instance)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            var blobName = $"\\{instance.StudyInstanceUid}\\{instance.SeriesInstanceUid}\\{instance.SopInstanceUid}_metadata";

            // Use the Azure storage SDK to validate the blob name; only specific values are allowed here.
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateBlobName(blobName);

            return _container.GetBlockBlobReference(blobName);
        }
    }
}
