// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    public class DicomBlobDataStore : IDicomBlobDataStore
    {
        private readonly CloudBlobContainer _container;
        private readonly ILogger<DicomBlobDataStore> _logger;

        public DicomBlobDataStore(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            ILogger<DicomBlobDataStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> InstanceExistsAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(dicomInstance);

            return await cloudBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    // Will throw if the provided resource identifier already exists.
                    return await blockBlob.ExistsAsync(cancellationToken);
                });
        }

        /// <inheritdoc />
        public async Task<Uri> AddInstanceAsStreamAsync(DicomInstance dicomInstance, Stream buffer, bool overwriteIfExists = false, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(buffer, nameof(buffer));
            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(dicomInstance);
            _logger.LogDebug($"Adding blob resource: {cloudBlob.Name}. Overwrite mode: {overwriteIfExists}.");

            return await cloudBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    // Will throw if the provided resource identifier already exists.
                    await blockBlob.UploadFromStreamAsync(
                            buffer,
                            overwriteIfExists ? AccessCondition.GenerateEmptyCondition() : AccessCondition.GenerateIfNotExistsCondition(),
                            new BlobRequestOptions(),
                            new OperationContext(),
                            cancellationToken);

                    return blockBlob.Uri;
                });
        }

        /// <inheritdoc />
        public async Task<Stream> GetInstanceAsStreamAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(dicomInstance);
            _logger.LogDebug($"Opening read of blob resource: {cloudBlob.Name}");

            return await cloudBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    return await blockBlob.OpenReadAsync(cancellationToken);
                });
        }

        /// <inheritdoc />
        public async Task DeleteInstanceIfExistsAsync(DicomInstance dicomInstance, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(dicomInstance);
            _logger.LogDebug($"Deleting blob resource: {cloudBlob.Name}");

            await cloudBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    await blockBlob.DeleteIfExistsAsync(cancellationToken);
                });
        }

        private CloudBlockBlob GetBlockBlobAndValidateName(DicomInstance dicomInstance)
        {
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));
            var blobName = $"{dicomInstance.StudyInstanceUID}/{dicomInstance.SeriesInstanceUID}/{dicomInstance.SopInstanceUID}";

            // Use the Azure storage SDK to validate the blob name; only specific values are allowed here.
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateBlobName(blobName);

            return _container.GetBlockBlobReference(blobName);
        }
    }
}
