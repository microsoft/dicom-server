// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    internal class DicomInstanceMetadataStore : IDicomInstanceMetadataStore
    {
        private readonly CloudBlobContainer _container;
        private readonly ILogger<DicomMetadataStore> _logger;
        private readonly Encoding _metadataEncoding;
        private readonly JsonSerializer _jsonSerializer;

        public DicomInstanceMetadataStore(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            ILogger<DicomMetadataStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _logger = logger;
            _metadataEncoding = Encoding.UTF8;
            _jsonSerializer = new JsonSerializer();
        }

        public async Task AddInstanceMetadataAsync(DicomDataset instanceMetadata, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(instanceMetadata, nameof(instanceMetadata));

            var dicomInstance = DicomInstance.Create(instanceMetadata);
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(dicomInstance);

            IAsyncPolicy retryPolicy = CreateTooManyRequestsRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Storing Instance Metadata: {dicomInstance}");

                    using (CloudBlobStream stream = await cloudBlockBlob.OpenWriteAsync(
                                                AccessCondition.GenerateIfExistsCondition(),
                                                new BlobRequestOptions(),
                                                new OperationContext(),
                                                cancellationToken))
                    using (var streamWriter = new StreamWriter(stream, _metadataEncoding))
                    using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                    {
                        _jsonSerializer.Serialize(jsonTextWriter, instanceMetadata);
                        jsonTextWriter.Flush();
                    }
                },
                retryPolicy);
        }

        public async Task DeleteInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken)
        {
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(instance);

            IAsyncPolicy retryPolicy = CreateTooManyRequestsRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Deleting Instance Metadata: {instance}");

                    // Attempt to delete, validating ETag
                    await cloudBlockBlob.DeleteAsync(
                        DeleteSnapshotsOption.IncludeSnapshots,
                        accessCondition: AccessCondition.GenerateIfExistsCondition(),
                        new BlobRequestOptions(),
                        new OperationContext(),
                        cancellationToken);
                },
                retryPolicy);
        }

        public async Task<DicomDataset> GetInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken)
        {
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(instance);

            return await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Getting Instance Metadata: {instance}");

                    using (Stream stream = await cloudBlockBlob.OpenReadAsync(cancellationToken))
                    using (var streamReader = new StreamReader(stream, _metadataEncoding))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        return _jsonSerializer.Deserialize<DicomDataset>(jsonTextReader);
                    }
                });
        }

        private static IAsyncPolicy CreateTooManyRequestsRetryPolicy()
           => Policy
                   .Handle<StorageException>(ex => ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.TooManyRequests ||
                                                    ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.BadRequest)
                   .RetryForeverAsync();

        private CloudBlockBlob GetInstanceBlockBlob(DicomInstance instance)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            var blobName = $"\\{instance.StudyInstanceUID}\\{instance.SeriesInstanceUID}\\{instance.SopInstanceUID}_metadata";

            // Use the Azure storage SDK to validate the blob name; only specific values are allowed here.
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateBlobName(blobName);

            return _container.GetBlockBlobReference(blobName);
        }
    }
}
