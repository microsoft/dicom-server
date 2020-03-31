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
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Blob.Features.Storage
{
    public class DicomBlobFileStore : IDicomFileStore
    {
        private readonly CloudBlobContainer _container;
        private readonly ILogger<DicomBlobFileStore> _logger;

        public DicomBlobFileStore(
            CloudBlobClient client,
            IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
            ILogger<DicomBlobFileStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
            EnsureArg.IsNotNull(logger, nameof(logger));

            BlobContainerConfiguration containerConfiguration = namedBlobContainerConfigurationAccessor.Get(Constants.ContainerConfigurationName);

            _container = client.GetContainerReference(containerConfiguration.ContainerName);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Uri> AddAsync(
            DicomInstanceIdentifier dicomInstanceIdentifier,
            Stream buffer,
            bool overwriteIfExists = false,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));
            EnsureArg.IsNotNull(buffer, nameof(buffer));

            var blobName = GetBlobStorageName(dicomInstanceIdentifier);

            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(blobName);
            _logger.LogDebug($"Adding blob resource: {blobName}. Overwrite mode: {overwriteIfExists}.");

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
        public async Task<Stream> GetAsync(DicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));

            var blobName = GetBlobStorageName(dicomInstanceIdentifier);
            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(blobName);
            _logger.LogDebug($"Opening read of blob resource: {blobName}");

            return await cloudBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    return await blockBlob.OpenReadAsync(cancellationToken);
                });
        }

        /// <inheritdoc />
        public async Task DeleteIfExistsAsync(DicomInstanceIdentifier dicomInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomInstanceIdentifier, nameof(dicomInstanceIdentifier));

            var blobName = GetBlobStorageName(dicomInstanceIdentifier);

            CloudBlockBlob cloudBlob = GetBlockBlobAndValidateName(blobName);
            _logger.LogDebug($"Deleting blob resource: {blobName}");

            await cloudBlob.CatchStorageExceptionAndThrowDataStoreException(
                async (blockBlob) =>
                {
                    await blockBlob.DeleteIfExistsAsync(cancellationToken);
                });
        }

        private CloudBlockBlob GetBlockBlobAndValidateName(string blobName)
        {
            EnsureArg.IsNotNullOrWhiteSpace(blobName, nameof(blobName));

            // Use the Azure storage SDK to validate the blob name; only specific values are allowed here.
            // Check here for more information: https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/
            NameValidator.ValidateBlobName(blobName);

            return _container.GetBlockBlobReference(blobName);
        }

        internal static string GetBlobStorageName(DicomInstanceIdentifier dicomInstance)
            => $"{dicomInstance.StudyInstanceUid}/{dicomInstance.SeriesInstanceUid}/{dicomInstance.SopInstanceUid}";
    }
}
