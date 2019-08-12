// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
using Microsoft.Health.Dicom.Metadata.Config;
using Newtonsoft.Json;
using Polly;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage
{
    internal class DicomInstanceMetadataStore : IDicomInstanceMetadataStore
    {
        private static readonly Encoding _metadataEncoding = Encoding.UTF8;
        private readonly DicomMetadataConfiguration _metadataConfiguration;
        private readonly CloudBlobContainer _container;
        private readonly JsonSerializer _jsonSerializer;
        private readonly ILogger<DicomInstanceMetadataStore> _logger;

        public DicomInstanceMetadataStore(
            CloudBlobClient client,
            JsonSerializer jsonSerializer,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            ILogger<DicomInstanceMetadataStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(jsonSerializer, nameof(jsonSerializer));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        /// <inheritdoc />
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

                    using (Stream stream = new MemoryStream())
                    using (var streamWriter = new StreamWriter(stream, _metadataEncoding))
                    using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                    {
                        _jsonSerializer.Serialize(jsonTextWriter, instanceMetadata);
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

        /// <inheritdoc />
        public async Task DeleteInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob cloudBlockBlob = GetInstanceBlockBlob(instance);

            IAsyncPolicy retryPolicy = CreateTooManyRequestsRetryPolicy();
            await cloudBlockBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    _logger.LogDebug($"Deleting Instance Metadata: {instance}");
                    await cloudBlockBlob.DeleteAsync(cancellationToken);
                },
                retryPolicy);
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetInstanceMetadataAsync(
            DicomInstance instance,
            HashSet<DicomAttributeId> optionalAttributes = null,
            CancellationToken cancellationToken = default)
        {
            DicomDataset instanceMetadata = await GetInstanceMetadataAsync(instance, cancellationToken);
            var resultDataset = new DicomDataset();

            // Create the collection of required attributes + requested attributes.
            HashSet<DicomAttributeId> attributes = _metadataConfiguration.InstanceRequiredMetadataAttributes;
            optionalAttributes?.Each(x => attributes.Add(x));

            foreach (DicomAttributeId seriesAttributeId in attributes)
            {
                TryAddDicomItem(resultDataset, instanceMetadata, seriesAttributeId);
            }

            return resultDataset;
        }

        /// <inheritdoc />
        public async Task<DicomDataset> GetInstanceMetadataAsync(DicomInstance instance, CancellationToken cancellationToken = default)
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

        private bool TryAddDicomItem(DicomDataset resultDataset, DicomDataset metadata, DicomAttributeId attributeId)
        {
            if (!metadata.TryGetDicomItems(attributeId, out DicomItem[] dicomItems))
            {
                return false;
            }

            if (dicomItems.Length > 1)
            {
                _logger.LogInformation($"Found more than one DICOM item for DICOM tag: {attributeId.FinalDicomTag}. The metadata results will have coalesced results.");
            }

            // Just add the first DICOM item; we might want to do something more clever in the future.
            resultDataset.Add(attributeId, dicomItems[0]);
            return true;
        }
    }
}
